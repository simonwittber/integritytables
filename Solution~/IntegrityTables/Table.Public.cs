using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IntegrityTables;

public delegate void RowActionFunc<T>(ref Row<T> data) where T : struct, IEquatable<T>;

public delegate bool RowConditionFunc<T>(in Row<T> data) where T : struct, IEquatable<T>;

public delegate void DataActionFunc<T>(ref T data) where T : struct;

public delegate bool DataConditionFunc<T>(in T data) where T : struct;

public partial class Table<T> : ITable<T> where T : struct, IEquatable<T>
{
    public string Name { get; }
    public Type DataType => typeof(T);
    public Type RowType => typeof(Row<T>);
    public Type ListType => typeof(List<Row<T>>);
    public IRowContainer<T> RowContainer => _rowContainer;

    
    public Table(IRowContainer<T>? rowContainer = null, IChangeSetLog<T>? changeLog = null, int capacity=1024)
    {
        _capacity = capacity;
        _keyGenerator = new TableKeyGenerator();
        _rowContainer = rowContainer ?? new RowContainer<T>();
        _changeSetLog = changeLog ?? new ChangeSetLog<T>();
        _rowContainer.OnRowModified += OnRowModified.Invoke;
        _rowObservers = new Dictionary<int, Action<Row<T>>?>(capacity);
        OnRowModified += DispatchObservers;
        Name = typeof(T).Name;
    }
    
    public int Count
    {
        get
        {
            lock (_sync) return _rowContainer.Count;
        }
    }

    public int ChangeSetCount
    {
        get
        {
            lock (_sync) return _changeSetLog.Count;
        }
    }

    public bool HasException
    {
        get
        {
            lock (_sync) return _changeSetLog.HasException;
        }
    }

    public Exception? Exception
    {
        get
        {
            lock (_sync) return _changeSetLog.Exception;
        }
    }
    
    public bool ContainsKey(int id)
    {
        lock (_sync)
            return _rowContainer.ContainsKey(id);
    }

    public bool Exists(RowConditionFunc<T> func)
    {
        lock (_sync)
        {
            for (var i = 0; i < Count; i++)
            {
                if (func(_rowContainer[i]))
                    return true;
            }

            return false;
        }
    }

    public bool Exists(DataConditionFunc<T> func) => Exists((in Row<T> row) => func(row.data));

    public bool Exists(Func<T, bool> func) => Exists((in Row<T> row) => func(row.data));

    public Row<T>[] ToArray()
    {
        lock (_sync)
        {
            var rows = new Row<T>[Count];
            var index = 0;
            foreach (var id in this)
            {
                var row = Get(id);
                row._index = index;
                rows[index] = row;
                index++;
            }

            return rows;
        }
    }

    public List<Row<T>> ToList()
    {
        lock (_sync)
        {
            return new List<Row<T>>(ToArray());
        }
    }

    public void Load(IList<object> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        if (rows.Count == 0) return;

        var typedRows = new Row<T>[rows.Count];
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i] is Row<T> row)
                typedRows[i] = row;
            else
                throw new InvalidCastException($"Row at index {i} is not of type {typeof(Row<T>).Name}, it is {rows[i]?.GetType().Name ?? "null"}.");
        }

        Load(typedRows);
    }

    public void Load(IList<Row<T>> rows)
    {
        lock (_sync)
        {
            Warnings.Log($"{typeof(T).Name}: Loading {rows.Count} rows, clearing existing {_rowContainer.Count} rows...");
            _rowContainer.Clear(rows.Count);
            foreach (var index in _indexes) index.Clear();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                try
                {
                    // TryAdd will return false if primary key is already taken. Other errors will throw.
                    if (!TryAdd(ref row, enableTriggers: false))
                    {
                        Warnings.Warn($"{typeof(T).Name}: Primary key already exists, failed to load row {i} with id {row.id} {row}");
                    }
                }
                // Catch errors from constraints
                catch (InvalidOperationException e)
                {
                    Warnings.Warn($"{typeof(T).Name}: Failed to load row {i} with id {row.id} {row} Exception: {e}");
                }

                Warnings.Log($"{typeof(T).Name}: Loaded row {i} with id {row.id} {row}");
                rows[i] = row;
            }

            ResetKeyGenerator();
        }
    }
}