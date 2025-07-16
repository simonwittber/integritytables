using System;

namespace IntegrityTables;
 
public class IdMap
{
    private int[] _map;

    public IdMap(int initialSize=10)
    {
        _map = new int[initialSize];
        Array.Fill(_map, -1);
    }

    public void Remove(int id)
    {
        if (id < 0 || id >= _map.Length) return;
        _map[id] = -1;
    }

    public bool ContainsKey(int id) => id >= 0 && id < _map.Length && _map[id] != -1;

    public int this[int id]
    {
        get => id < 0 || id >= _map.Length? -1 : _map[id];
        set
        {
            if (id >= _map.Length)
            {
                var oldMapLength = _map.Length;
                var newSize = oldMapLength;
                while (newSize <= id) newSize <<= 1;
                Array.Resize(ref _map, newSize);
                Array.Fill(_map, -1, oldMapLength, newSize - oldMapLength);
            }
            _map[id] = value;
        }
    }

    public bool TryGetValue(int id, out int index)
    {
        if(id < 0 || id >= _map.Length || _map[id] == -1)
        {
            index = -1;
            return false;
        }
        index = _map[id];
        return true;
    }

    public void Clear() => Array.Fill(_map, -1);
}