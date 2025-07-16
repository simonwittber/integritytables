using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace IntegrityTables;

public class RowModifiedActionList
{
    private List<Action<int, TableOperation>?> _actions = new();
    private ConcurrentQueue<(int, TableOperation)> _operations = new();
    
    public static RowModifiedActionList operator +(RowModifiedActionList e, Action<int, TableOperation>? item)
    {
        e._actions.Add(item);
        return e;
    }

    public static RowModifiedActionList operator -(RowModifiedActionList e, Action<int, TableOperation>? item)
    {
        e._actions.Remove(item);
        return e;
    }
    
    public void Invoke(int index, TableOperation operation)
    {
        foreach (var action in _actions)
        {
            action?.Invoke(index, operation);
        }
    }

    public void Enqueue(int index, TableOperation operation)
    {
        _operations.Enqueue((index, operation));
    }

    public void Flush()
    {
        var count = _operations.Count;
        for(var i=0; i<count; i++)
        {
            if (!_operations.TryDequeue(out var operation))
                break;
            var (index, op) = operation;
            foreach(var action in _actions) action?.Invoke(index, op);
        }
    }
}