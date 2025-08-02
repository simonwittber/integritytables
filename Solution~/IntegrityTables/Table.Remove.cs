using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    public void Remove(int id, CascadeOperation cascadeOperation = CascadeOperation.None)
    {
        if (!TryRemove(id, cascadeOperation))
        {
            RaiseException(new KeyNotFoundException($"No row with id {id}."));
        }
    }

    public bool TryRemove(int id, CascadeOperation cascadeOperation = CascadeOperation.None)
    {
        using(_lock.WriteScope())
        {
            if (ContainsKey(id) == false) return false;
            ExecuteCascadingRemove?.Invoke(id, cascadeOperation);
            CheckChangeSetState();
            if (id == 0)
                return false;
            if (!TryGet(id, out var row))
                return false;
            if (_isInChangeSet) _changeSetLog.RegisterRemove(row);
            var entered = false;
            try
            {
                TriggerGuard.Enter(GetType());
                entered = true;
                BeforeRemove.Invoke(in row);
                foreach (var index in _indexes) index.Remove(in row);
                _rowContainer.Remove(in row);
                if (_rowContainer.Count == 0)
                {
                    _changeSetLog.RegisterKeyGeneratorReset(_keyGenerator.CurrentKey);
                    _keyGenerator.Reset(0);
                }

                AfterRemove.Invoke(in row);
            }
            finally
            {
                if (entered)
                    TriggerGuard.Exit(GetType());
            }

            return true;
        }
    }

    public void Remove(in Row<T> row, CascadeOperation cascadeOperation = CascadeOperation.None)
    {
        if (!TryRemove(row.id, cascadeOperation))
            RaiseException(new InvalidOperationException("Row has id = 0 (null)"));
    }

    public bool TryRemove(in Row<T> row, CascadeOperation cascadeOperation = CascadeOperation.None) => TryRemove(row.id, cascadeOperation);

    public void Remove(RowConditionFunc<T> condition, CascadeOperation cascadeOperation = CascadeOperation.None)
    {
        // TODO: should this be inside a change set, or should we leave that to the caller?
        using(_lock.WriteScope())
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var row = _rowContainer[i];
                if (condition(in row))
                {
                    Remove(row.id, cascadeOperation);
                }
            }
        }
        
    }

    public void Clear(CascadeOperation cascadeOperation = CascadeOperation.None)
    {
        using(_lock.WriteScope())
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var row = _rowContainer[i];
                Remove(row.id, cascadeOperation);
            }
        }
        
    }
}