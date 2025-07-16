using System;
using System.Collections.Generic;

namespace IntegrityTables;

public struct QueryEnumerator<T> where T : struct, IEquatable<T>
{
    private int _index = -1;
    private readonly IRowContainer<T> _rowContainer;
    private readonly RowConditionFunc<T>? _func;
    private int _version;

    public QueryEnumerator(Table<T> table, RowConditionFunc<T>? func)
    {
        _rowContainer = table._rowContainer;
        _func = func;
        _index = -1;
        _version = _rowContainer.Version;
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
        while (true)
        {
            _index++;
            if (_index >= _rowContainer.Count)
                return false;
            var row = Current;
            if (_func == null || _func(in row))
            {
                return true;
            }
        }
    }

    public void Reset()
    {
        _index = -1;
    }

    public QueryEnumerator<T> GetEnumerator()
    {
        return this;
    }

    public List<Row<T>> ToList()
    {
        var list = new List<Row<T>>();
        while (MoveNext()) list.Add(Current);
        return list;
    }

    public Row<T>[] ToArray()
    {
        return ToList().ToArray();
    }
}