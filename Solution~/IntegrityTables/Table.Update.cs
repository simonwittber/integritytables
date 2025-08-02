using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    private enum UpdateResult
    {
        Success,
        NotFound,
        StaleVersion,
        NoChange
    }

    public void Update(RowObjectAdapter adapter)
    {
        if (adapter.row is Row<T> typedRow)
        {
            Update(ref typedRow);
            adapter.row = typedRow;
            return;
        }

        throw new InvalidOperationException($"RowObjectAdapter row type mismatch. Expected {typeof(Row<T>)} but got {adapter.row.GetType()}");
    }

    public bool TryUpdate(RowObjectAdapter adapter)
    {
        if (adapter.row is Row<T> typedRow)
        {
            var result = TryUpdate(ref typedRow);
            if (result)
                adapter.row = typedRow;
            return result;
        }

        throw new InvalidOperationException($"RowObjectAdapter row type mismatch. Expected {typeof(Row<T>)} but got {adapter.row.GetType()}");
    }

    public void Update(RowActionFunc<T> fn, RowConditionFunc<T> where = null)
    {
        // TODO: should this be inside a change set, or should we leave that to the caller?
        using(_lock.WriteScope())
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var row = _rowContainer[i];
                if (where != null && !where(in row)) continue;
                fn(ref row);
                Update(ref row);
            }
        }
        
    }

    public void Update(ref Row<T> row)
    {
        var result = TryUpdateInternal(ref row);
        switch (result)
        {
            case UpdateResult.NoChange:
            case UpdateResult.Success:
                break;
            case UpdateResult.NotFound:
                RaiseException(new KeyNotFoundException($"No row with id {row.id}."));
                break;
            case UpdateResult.StaleVersion:
                RaiseException(new InvalidOperationException($"Row has been modified by another transaction [{row}]"));
                break;
        }
    }

    public bool TryUpdate(ref Row<T> row)
    {
        var result = TryUpdateInternal(ref row);
        return result is UpdateResult.Success or UpdateResult.NoChange;
    }

    private UpdateResult TryUpdateInternal(ref Row<T> row)
    {
        using(_lock.WriteScope())
        {
            if (!TryGet(row.id, out var storedRow))
                return UpdateResult.NotFound;

            ValidateForUpdate?.Invoke(row.data, storedRow.data);
            CheckChangeSetState();

            if (row.id == 0 || !TryGet(row.id, out var existingRow))
                return UpdateResult.NotFound;

            if (EqualityComparer<Row<T>>.Default.Equals(existingRow, row))
                return UpdateResult.NoChange;

            if (existingRow._version > row._version)
                return UpdateResult.StaleVersion;

            row._version++;

            var entered = false;
            try
            {
                TriggerGuard.Enter(GetType());
                entered = true;
                if (_isInChangeSet) _changeSetLog.RegisterUpdate(existingRow, row);
                BeforeUpdate.Invoke(in existingRow, ref row);
                CheckConstraints(in row);
                foreach (var index in _indexes)
                    index.Update(in existingRow, in row);
                _rowContainer.Set(ref row);
                AfterUpdate.Invoke(in existingRow, in row);
            }
            finally
            {
                if (entered)
                    TriggerGuard.Exit(GetType());
            }

            return UpdateResult.Success;
        }
        
    }
}