using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    public Row<T> Get(int id)
    {
        if (!TryGet(id, out var row))
            RaiseException(new KeyNotFoundException($"No row with id {id}."));
        return row;
    }

    public bool TryGet(int id, out Row<T> row)
    {
        using (_lock.ReadScope())
        {
            if (!_rowContainer.TryGetIndexForId(id, out var index))
            {
                row = default;
                return false;
            }

            row = _rowContainer[index];
            return true;
        }
    }

    /// <summary>
    /// Get a row by its index in the table (not the id).
    /// </summary>
    /// <param name="index"></param>
    public Row<T> this[int index] => _rowContainer[index];

    /// <summary>
    /// Get a RowObjectAdapter for the row at the specified index (not the id).
    /// </summary>
    /// <param name="index"></param>
    /// <exception cref="InvalidOperationException"></exception>
    RowObjectAdapter ITable.this[int index]
    {
        get
        {
            if (Metadata != null) return new RowObjectAdapter(Metadata, _rowContainer[index]);
            throw new InvalidOperationException("Metadata is not set. Cannot create RowObjectAdapter.");
        }
    }

    public bool TryGetOne(Func<T, bool> func, out Row<T> result)
    {
        using (_lock.ReadScope())

        {
            for (var i = 0; i < Count; i++)
            {
                var row = _rowContainer[i];
                if (!func(row.data))
                    continue;
                result = row;
                return true;
            }

            result = default;
            return false;
        }
    }

    public Row<T> GetOne(Func<T, bool> func)
    {
        using (_lock.ReadScope())
        {
            if (TryGetOne(func, out var row))
                return row;
            throw new KeyNotFoundException("Could not find row.");
        }
    }

    // TODO: expose any [Unique] generated indexes as properties rather than lookup by string name.
    public bool TryGetByUniqueIndex<TK>(string indexName, TK key, out Row<T> row) where TK : notnull
    {
        using (_lock.ReadScope())
        {
            // find the matching unique‐index
            if (_indexMap.TryGetValue(indexName, out var index))
            {
                if (index is UniqueIndex<T, ValueTuple<TK>> indexForKeyType)
                {
                    var tupleKey = ValueTuple.Create(key);
                    if (indexForKeyType.TryGet(tupleKey, out var id))
                        return TryGet(id, out row);
                    row = default;
                    return false;
                }
                else
                {
                    throw new InvalidOperationException($"Unique index {indexName} is not of type {typeof(UniqueIndex<T, TK>).Name}");
                }
            }

            throw new KeyNotFoundException("Could not find unique index.");
        }
    }

    public Row<T> GetByUniqueIndex<TU>(string indexName, TU key) where TU : notnull
    {
        if (!TryGetByUniqueIndex(indexName, key, out var row))
            throw new KeyNotFoundException($"No {{typeof(T).Name}} for {indexName} = {{key}}");
        return row;
    }
}