using System;
using System.Collections.Generic;

namespace IntegrityTables;

public class UniqueIndex<T, TU> : IUniqueIndex<T> where T : struct, IEquatable<T> where TU : notnull
{
    public delegate TU GetKeyDelegate(in Row<T> row);

    private readonly GetKeyDelegate _getKeyFunc;

    private readonly Dictionary<TU, int> _index;

    private readonly string _name;
    
    public string Name => _name;

    private readonly Table<T> _table;

    public UniqueIndex(Table<T> table, string name, GetKeyDelegate getKeyFunc, int capacity=1024)
    {
        _name = name;
        _getKeyFunc = getKeyFunc;
        _table = table;
        _index = new Dictionary<TU, int>(capacity);
    }

    public void Add(in Row<T> row)
    {
        var key = _getKeyFunc(in row);
        if (!_index.TryAdd(key, row.id)) _table.RaiseException($"{typeof(T).FullName}: Unique constraint violation on index '{_name}' for key '{key}'.");
    }

    public void Remove(in Row<T> row)
    {
        var key = _getKeyFunc(in row);
        _index.Remove(key);
    }

    public void Update(in Row<T> oldRow, in Row<T> newRow)
    {
        var oldKey = _getKeyFunc(in oldRow);
        var newKey = _getKeyFunc(in newRow);
        if (EqualityComparer<TU>.Default.Equals(oldKey, newKey)) return;
        Add(in newRow);
        Remove(in oldRow);
    }

    public void Clear()
    {
        _index.Clear();
    }

    public bool TryGet(TU key, out int rowIndex)
    {
        return _index.TryGetValue(key, out rowIndex);
    }
}