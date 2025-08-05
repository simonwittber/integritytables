using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public class IntMap<T>
{
    
    private const int PageBits = 10;          
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;

    private readonly List<T[]?> _values = new List<T[]?>();
    private readonly IntSet _keys = new IntSet();

    private void EnsurePage(int pageIndex)
    {
        while (_values.Count <= pageIndex)
        {
            _values.Add(null);
        }
        if (_values[pageIndex] == null)
        {
            _values[pageIndex] = new T[PageSize];
        }
    }

    public bool Remove(int v)
    {
        if (!_keys.Remove(v)) return false;
        var u = ZigZagEncode(v);
        var pageIndex = (int) (u >> PageBits);
        var slot = (int)(u & PageMask);
        _values[pageIndex]![slot] = default(T);
        return true;
    }

    public bool ContainsKey(int id) => _keys.Contains(id);

    public bool TryGetValue(int v, out T value)
    {
        if (_keys.Contains(v))
        {
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            var slot = key & PageMask;
            value = _values[pageIndex]![slot];
            return true;
        }
        value = default(T);
        return false;
    }
    
    public T this[int v]
    {
        get
        {
            if (!_keys.Contains(v)) 
                throw new KeyNotFoundException();
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            var slot = key & PageMask;
            return _values[pageIndex]![slot];
        }
        set
        {
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            EnsurePage(pageIndex);
            var slot = key & PageMask;
            _values[pageIndex]![slot] = value;
            _keys.Add(v);
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
        _keys.Clear();
    }

    public IntSet.Enumerator GetEnumerator() => _keys.GetEnumerator();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZigZagEncode(int v) => ((uint) (v << 1)) ^ ((uint) (v >> 31));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ZigZagDecode(uint u) => (int) ((u >> 1) ^ -(u & 1));
}