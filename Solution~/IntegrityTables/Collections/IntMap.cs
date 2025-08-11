using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public interface IReadOnlyIntMap<T>
{
    int Count { get; }
    IntMap<T>.ValueEnumerator Values { get; }
    bool ContainsKey(int id);
    bool TryGetValue(int v, out T value);
    IntSet.Enumerator GetEnumerator();
    T this[int v] { get; }
    IReadOnlyIntSet Keys { get; }
}

public class IntMap<T> : IReadOnlyIntMap<T>
{
    private const int PageBits = 10;
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;

    private readonly List<T[]> _values = new List<T[]>();
    private readonly IntSet _keys = new IntSet();

    private int _initialKey;
    private bool _isInitialized;
    
    public IReadOnlyIntSet Keys => _keys;
    
    public int Count { get; private set; }

    private void EnsurePage(int pageIndex)
    {
        while (_values.Count <= pageIndex)
        {
            _values.Add(null);
        }

        _values[pageIndex] ??= new T[PageSize];
    }

    public void EnsureCapacity(int items)
    {
        // NOP, as capacity is managed dynamically based on key range, not number of items
    }

    public bool Remove(int v)
    {
        if (!_keys.Remove(v)) return false;
        var u = ZigZagEncode(v - _initialKey);
        var pageIndex = (int) (u >> PageBits);
        var slot = (int) (u & PageMask);
        _values[pageIndex]![slot] = default(T)!;
        Count--;
        return true;
    }

    public bool ContainsKey(int id) => _keys.Contains(id);

    public bool TryGetValue(int v, out T value)
    {
        if (_keys.Contains(v))
        {
            var key = ZigZagEncode(v - _initialKey);
            var pageIndex = (int)(key >> PageBits);
            var slot = (int)(key & PageMask);
            value = _values[pageIndex]![slot];
            return true;
        }

        value = default(T)!;
        return false;
    }

    public void Add(int key, T item)
    {
        if (_keys.Contains(key))
            throw new InvalidOperationException("Key already exists");
        this[key] = item;
    }

    public void TryAdd(int key, T item)
    {
        if (!ContainsKey(key))
            this[key] = item;
    }

    public T this[int v]
    {
        get
        {
            if (!_keys.Contains(v))
                throw new KeyNotFoundException();
            var key = ZigZagEncode(v - _initialKey);
            var pageIndex = (int)(key >> PageBits);
            var slot = (int)(key & PageMask);
            return _values[pageIndex]![slot];
        }
        set
        {
            _initialKey = _isInitialized ? _initialKey : v;
            _isInitialized = true;
            var key = ZigZagEncode(v - _initialKey);
            var pageIndex = (int)(key >> PageBits);
            EnsurePage(pageIndex);
            var slot = (int)(key & PageMask);
            _values[pageIndex]![slot] = value;
            if (_keys.Add(v))
                Count++;
        }
    }


    public void Clear()
    {
        foreach (var page in _values)
        {
            if (page != null)
            {
                Array.Fill(page, default);
            }
        }

        _isInitialized = false;
        _initialKey = 0;
        Count = 0;
        _keys.Clear();
    }

    public IntSet.Enumerator GetEnumerator() => _keys.GetEnumerator();

    public ValueEnumerator Values => new ValueEnumerator(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZigZagEncode(int v) => ((uint) (v << 1)) ^ ((uint) (v >> 31));

    public struct ValueEnumerator
    {
        IntMap<T> _map;
        int _pageIndex;
        int _slotIndex;
        T[] activePage;
        private IntSet.Enumerator _keyEnumerator;

        public ValueEnumerator GetEnumerator() => this;

        public T Current { get; private set; }

        public ValueEnumerator(IntMap<T> map)
        {
            _map = map;
            _keyEnumerator = map._keys.GetEnumerator();
            _pageIndex = 0;
            _slotIndex = 0;
            activePage = null;
        }

        public bool MoveNext()
        {
            // Move to next key
            if (_keyEnumerator.MoveNext())
            {
                var key = _keyEnumerator.Current;
                Current = _map[key];
                return true;
            }
            return false;
        }
    }
}