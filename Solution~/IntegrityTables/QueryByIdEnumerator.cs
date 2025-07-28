using System;
using System.Collections.Generic;

namespace IntegrityTables;

public struct QueryByIdEnumerator<T> where T : struct, IEquatable<T>
{
    private int _index = -1;
    private readonly IRowContainer<T> _rowContainer;
    private readonly IList<int> _ids;
    private int _version;

    public QueryByIdEnumerator(Table<T> table, IList<int> ids)
    {
        _rowContainer = table._rowContainer;
        _ids = ids;
        _index = -1;
        _version = _rowContainer.Version;
    }
    
    void ThrowIfVersionChanged()
    {
        if (_version != _rowContainer.Version)
            throw new InvalidOperationException("The table has been modified during enumeration.");
    }

    public Row<T> Current => _rowContainer[_rowContainer.GetIndexForId(_ids[_index])];

    public bool MoveNext()
    {
        ThrowIfVersionChanged();
        
        _index++;
        return _index < _ids.Count;
    }

    public void Reset()
    {
        _index = -1;
    }

    public QueryByIdEnumerator<T> GetEnumerator()
    {
        return this;
    }

    public List<Row<T>> ToList()
    {
        var list = new List<Row<T>>();
        while (MoveNext()) 
        {
            list.Add(Current);
        }
        return list;
    }

    public Row<T>[] ToArray()
    {
        return ToList().ToArray();
    }
}
