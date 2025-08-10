using System;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

/// <summary>
/// A dynamic mapping from arbitrary int keys to dense 0-based IDs.
/// </summary>
public class DenseIdMap : IDisposable
{
    private int[] _keys; // stored keys
    private int[] _values; // stored id+1; 0 indicates empty, -1 indicates deleted
    private int[] _idToValue; // inverse map: id -> original key
    private int _mask; // _capacity - 1 for fast modulo
    private int _count; // Number of items
    private int _deletedCount; // Number of tombstone entries
    private bool _disposed;

    private const int EMPTY = 0; // Empty slot marker
    private const int DELETED = -1; // Tombstone marker

    public int Count => _count;

    /// <summary>
    /// Constructs a ValueToIdMap with the given initial capacity (will be rounded up to nearest power of two).
    /// </summary>
    public DenseIdMap(int initialCapacity = 16)
    {
        if (initialCapacity < 1) initialCapacity = 16;

        // Round up to power of 2
        int capacity = 1;
        while (capacity < initialCapacity)
            capacity <<= 1;

        _keys = new int[capacity];
        _values = new int[capacity];
        _mask = capacity - 1;
        _idToValue = new int[16];
        _count = 0;
        _deletedCount = 0;
    }

    public DenseIdMap()
    {
        // Round up to power of 2
        int capacity = 1;
        while (capacity < 16)
            capacity <<= 1;

        _keys = new int[capacity];
        _values = new int[capacity];
        _mask = capacity - 1;
        _idToValue = new int[16];
        _count = 0;
        _deletedCount = 0;
    }

    public DenseIdMap(int[] keys) : this(keys.Length)
    {
        foreach (var key in keys)
            GetOrAdd(key);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint Hash(int x) => (uint) x * 0x9E3779B1u;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool FindSlot(int value, out uint index, out bool exists)
    {
        // this is equivalent to Hash(value) % capacity, because capacity is power of 2
        uint startIndex = Hash(value) & (uint) _mask;
        index = startIndex;
        uint firstTombstoneIndex = uint.MaxValue; // Track first tombstone for insertion

        while (true) // NOTE: This loop will always terminate because Resize() and CleanupTombstones() ensure empty space
        {
            int stored = _values[index];
            if (stored == EMPTY) // Empty slot
            {
                exists = false;
                // Use tombstone slot if available, otherwise use empty slot
                if (firstTombstoneIndex != uint.MaxValue)
                    index = firstTombstoneIndex;
                return true;
            }

            if (stored == DELETED) // Tombstone
            {
                if (firstTombstoneIndex == uint.MaxValue)
                    firstTombstoneIndex = index; // Remember first tombstone
            }
            else if (_keys[index] == value) // Found existing (not deleted)
            {
                exists = true;
                return true;
            }

            index = (index + 1) & (uint) _mask;

            /*
            // This is not needed and removed for optimisation
            if (idx == startIndex) // have looped back and searched entire array
            {
                exists = false;
                return false;
            }
            */
        }
    }

    /// <summary>
    /// Gets the existing ID for the value or assigns a new one.
    /// </summary>
    public int GetOrAdd(int value)
    {
        FindSlot(value, out uint idx, out bool exists);

        if (exists)
            return _values[idx] - 1;

        // Add new entry
        int id = _count++;
        _keys[idx] = value;
        _values[idx] = id + 1;

        // Resize if load factor too high
        if (_count + _deletedCount > (_keys.Length * 3) / 4)
        {
            Resize();
        }

        // Ensure inverse map capacity
        if (_count > _idToValue.Length)
        {
            Array.Resize(ref _idToValue, _idToValue.Length * 2);
        }

        _idToValue[id] = value;
        return id;
    }

    /// <summary>
    /// Checks if the value exists in the map.
    /// </summary>
    public bool Contains(int value)
    {
        FindSlot(value, out _, out bool exists);
        return exists;
    }

    /// <summary>
    /// Tries to get the ID for the value.
    /// </summary>
    public bool TryGetId(int value, out int id)
    {
        FindSlot(value, out uint idx, out bool exists);
        if (exists)
        {
            id = _values[idx] - 1;
            return true;
        }

        id = -1;
        return false;
    }

    /// <summary>
    /// Gets the original value for the given ID.
    /// </summary>
    public int GetValue(int id)
    {
        if ((uint) id >= (uint) _count)
            throw new ArgumentOutOfRangeException(nameof(id));
        return _idToValue[id];
    }

    public bool TryGetValue(int id, out int value)
    {
        if ((uint) id >= (uint) _count)
        {
            value = -1;
            return false;
        }

        value = _idToValue[id];
        return true;
    }

    /// <summary>
    /// Removes a value from the map. Returns true if the value was found and removed.
    /// </summary>
    public bool Remove(int value)
    {
        uint idx = Hash(value) & (uint) _mask;

        while (true)
        {
            int stored = _values[idx];
            if (stored == EMPTY) // Not found
                return false;

            if (stored != DELETED && _keys[idx] == value) // Found existing (not deleted)
            {
                // Mark as deleted (tombstone)
                _values[idx] = DELETED;
                _deletedCount++;
                _count--;

                // Trigger cleanup if too many tombstones
                if (_deletedCount > _keys.Length / 4)
                {
                    CleanupTombstones();
                }

                return true;
            }

            idx = (idx + 1) & (uint) _mask;
        }
    }

    private void Resize()
    {
        int newCapacity = _keys.Length * 2;
        var oldKeys = _keys;
        var oldValues = _values;

        _keys = new int[newCapacity];
        _values = new int[newCapacity];
        _mask = newCapacity - 1;

        // Rehash all non-deleted entries
        for (int i = 0; i < oldKeys.Length; i++)
        {
            int stored = oldValues[i];
            if (stored > 0) // Valid entry (not empty or deleted)
            {
                int key = oldKeys[i];

                // Find new position
                uint idx = Hash(key) & (uint) _mask;
                while (_values[idx] != EMPTY)
                    idx = (idx + 1) & (uint) _mask;

                _keys[idx] = key;
                _values[idx] = stored;
            }
        }

        _deletedCount = 0; // Cleanup removes all tombstones
    }

    /// <summary>
    /// Cleans up tombstones by rebuilding the hash table without deleted entries.
    /// </summary>
    private void CleanupTombstones()
    {
        var oldKeys = _keys;
        var oldValues = _values;

        // Clear current arrays
        Array.Clear(_keys, 0, _keys.Length);
        Array.Clear(_values, 0, _values.Length);

        // Rehash all non-deleted entries
        for (int i = 0; i < oldKeys.Length; i++)
        {
            int stored = oldValues[i];
            if (stored > 0) // Valid entry (not empty or deleted)
            {
                int key = oldKeys[i];

                // Find new position
                uint idx = Hash(key) & (uint) _mask;
                while (_values[idx] != EMPTY)
                    idx = (idx + 1) & (uint) _mask;

                _keys[idx] = key;
                _values[idx] = stored;
            }
        }

        _deletedCount = 0;
    }

    /// <summary>
    /// Returns an enumerator that iterates through all values in ID order.
    /// </summary>
    public Enumerator GetEnumerator() => new Enumerator(this);

    /// <summary>
    /// Enumerator for ValueToIdMap that iterates over values in ID order.
    /// </summary>
    public struct Enumerator
    {
        private readonly DenseIdMap _map;
        private int _currentId;

        public int Current { get; private set; }

        internal Enumerator(DenseIdMap map)
        {
            _map = map;
            _currentId = -1;
            Current = 0;
        }

        public bool MoveNext()
        {
            _currentId++;
            if (_currentId < _map._count)
            {
                Current = _map._idToValue[_currentId];
                return true;
            }

            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _keys = null;
            _values = null;
            _idToValue = null;
            _disposed = true;
        }
    }

    public void Clear()
    {
        Array.Clear(_keys, 0, _keys.Length);
        Array.Clear(_values, 0, _values.Length);
        Array.Clear(_idToValue, 0, _idToValue.Length);

        _count = 0;
        _deletedCount = 0;
        _disposed = false;

        // Reset mask
        _mask = _keys.Length - 1;
    }
}
