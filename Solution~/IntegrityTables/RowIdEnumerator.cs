using System;
using System.Collections.Generic;

namespace IntegrityTables;

public struct RowIdEnumerator<T> where T : struct, IEquatable<T>
{
    private int _index = -1;
    private readonly IRowContainer<T> _rowContainer;
    private int _version;

    public RowIdEnumerator(Table<T> table)
    {
        _rowContainer = table._rowContainer;
        _index = -1;
        _version = table._rowContainer.Version;
    }
    
    void ThrowIfVersionChanged()
    {
        if (_version != _rowContainer.Version)
            throw new InvalidOperationException("The table has been modified during enumeration.");
    }

    public int Current => _rowContainer[_index].id;

    public bool MoveNext()
    {
        ThrowIfVersionChanged();
        _index++;
        if (_index >= _rowContainer.Count)
            return false;
        return true;
    }

    public void Reset()
    {
        _index = -1;
    }

    public RowIdEnumerator<T> GetEnumerator()
    {
        return this;
    }

    public int[] ToArray()
    {
        var ids = new int[_rowContainer.Count];
        for (var i = 0; i < _rowContainer.Count; i++) ids[i] = _rowContainer[i].id;
        return ids;
    }

    public List<int> ToList()
    {
        var ids = new List<int>(_rowContainer.Count);
        for (var i = 0; i < _rowContainer.Count; i++) ids.Add(_rowContainer[i].id);
        return ids;
    }
}