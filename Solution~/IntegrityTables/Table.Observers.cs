using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T>
{
    private readonly Dictionary<int, Action<Row<T>>?> _rowObservers;
    /// <summary>
    /// Register a function to be called when a row is modified. (int index, TableOperation op)
    /// Note that the index argument is the index in the row container, not the id of the row.
    /// </summary>
    public RowModifiedActionList OnRowModified { get; set; } = new();

    // This method is registered to be called when a row is modified using the OnRowModified action list.
    // It dispatches observers for the row at the given index based on the operation performed.
    private void DispatchObservers(int index, TableOperation op)
    {
        if (op != TableOperation.Update) return;
        if (_rowContainer.TryGetIdForIndex(index, out var id))
        {
            if (_rowObservers.TryGetValue(id, out var observers))
            {
                var row = _rowContainer[index];
                observers?.Invoke(row);
            }
        }
    }

    // This method is used to add an observer for a specific row in the table.
    // It returns a Disposable which can be used to remove the observer later.
    public IDisposable AddObserver(Row<T> row, Action<Row<T>>? onUpdated)
    {
        if (_rowObservers.TryGetValue(row.id, out var observer))
        {
            observer -= onUpdated;
            observer += onUpdated;
            _rowObservers[row.id] = observer;
        }
        else
            _rowObservers[row.id] = onUpdated;

        var disposableObserver = new DisposableObserver<T>()
        {
            Table = this,
            Row = row,
            OnUpdated = onUpdated,
        };
        return disposableObserver;
    }
    
    // This method is used to remove an observer for a specific row in the table.
    // Instead of using the Disposable returned by AddObserver, you can call this method directly.
    public void RemoveObserver(Row<T> row, Action<Row<T>>? onUpdated)
    {
        if (_rowObservers.TryGetValue(row.id, out var observer))
        {
            observer -= onUpdated;
            if (observer == null)
                _rowObservers.Remove(row.id);
            else
                _rowObservers[row.id] = observer;
        }
    }

    
}