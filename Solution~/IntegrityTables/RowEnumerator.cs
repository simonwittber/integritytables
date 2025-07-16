using System;
using System.Collections.Generic;

namespace IntegrityTables;

public struct RowEnumerator<T> where T : struct, IEquatable<T>
{
    private int _index = -1;
    private readonly IRowContainer<T> _rowContainer;
    private int _version;

    public RowEnumerator(Table<T> table)
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
    
    public Row<T> Current => _rowContainer[_index];

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

    public RowEnumerator<T> GetEnumerator()
    {
        return this;
    }

    public Row<T>[] ToArray()
    {
        var rows = new Row<T>[_rowContainer.Count];
        for (var i = 0; i < _rowContainer.Count; i++) rows[i] = _rowContainer[i];
        return rows;
    }

    public List<Row<T>> ToList()
    {
        var rows = new List<Row<T>>(_rowContainer.Count);
        for (var i = 0; i < _rowContainer.Count; i++) rows.Add(_rowContainer[i]);
        return rows;
    }
}

