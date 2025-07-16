using System;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    public RowObjectAdapter CreateEmptyRow()
    {
        var row = new Row<T>(0, default);
        if (Metadata != null) return new RowObjectAdapter(Metadata, row);
        throw new InvalidOperationException("Metadata is not set. Cannot create empty row.");
    }
    
    public void Add(RowObjectAdapter adapter)
    {
        if (adapter.row is Row<T> typedRow)
        {
            Add(ref typedRow);
            adapter.row = typedRow;
            return;
        }
        throw new InvalidOperationException($"RowObjectAdapter row type mismatch. Expected {typeof(Row<T>)} but got {adapter.row.GetType()}");
    }

    public bool TryAdd(RowObjectAdapter adapter)
    {
        if (adapter.row is Row<T> typedRow)
        {
            var result = TryAdd(ref typedRow);
            if(result)
                adapter.row = typedRow;
            return result;
        }

        throw new InvalidOperationException($"RowObjectAdapter row type mismatch. Expected {typeof(Row<T>)} but got {adapter.row.GetType()}");
    }

    
    public Row<T> Add(T data)
    {
        var row = new Row<T>(0, data);
        Add(ref row);
        return row;
    }

    public void Add(ref Row<T> row)
    {
        if (!TryAdd(ref row))
            throw new InvalidOperationException($"Row with id {row.id} already exists.");
    }

    public bool TryAdd(ref Row<T> row, bool enableTriggers=true, bool enableConstraints=true)
    {
        lock(_sync)
        {
            if(enableTriggers) ValidateForAdd?.Invoke(row.data);
            CheckChangeSetState();
            if (row.id == 0)
            {
                row.id = _keyGenerator.NextId();
            }
            else
            {
                if (ContainsKey(row.id))
                    return false;
                _keyGenerator.EnsureAtLeast(row.id);
            }

            var entered = false;
            try
            {
                TriggerGuard.Enter(GetType());
                entered = true;
                BeforeAdd.Invoke(ref row, enableTriggers);
                if (enableConstraints) CheckConstraints(in row);
                foreach (var index in _indexes)
                    index.Add(in row);
                _rowContainer.Add(ref row);
                AfterAdd.Invoke(in row, enableTriggers);
                if (_isInChangeSet) _changeSetLog.RegisterAdd(row);
            }
            finally
            {
                if(entered)
                    TriggerGuard.Exit(GetType());
            }

            return true;
        }
    }
}