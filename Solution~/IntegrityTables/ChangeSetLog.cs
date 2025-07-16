using System;
using System.Collections.Generic;

namespace IntegrityTables;

internal class ChangeSetLog<T> : IChangeSetLog<T> where T : struct, IEquatable<T>
{
    private enum LogType
    {
        Add,
        Update,
        Remove,
        KeyGeneratorReset
    }

    private readonly Stack<(LogType, Row<T>)> _entries = new(32);
    private readonly Stack<Row<T>> _updateEntries = new(32);
    private readonly Stack<int> _changeSets = new(32);
    
    public Exception? Exception { get; set; }
    
    public bool IsActive => Count > 0 || _changeSets.Count > 0;

    public bool HasException => Exception != null;
    public bool IsEmpty => _entries.Count == 0;

    public int Count => _entries.Count;


    // The undo log records the original state of data before a change is made
    private void Register(LogType type, Row<T> item)
    {
        _entries.Push((type, item));
    }

    public void Discard()
    {
        if (_changeSets.Count == 0)
            throw new InvalidOperationException("No active changeset to commit.");
        // pop off this level’s marker
        _changeSets.Pop();
        
        if (_changeSets.Count == 0)
        {
            Exception = null;
            _entries.Clear();
            _updateEntries.Clear();
        }
    }

    public void Rollback(Table<T> table)
    {
        if (_changeSets.Count == 0)
            throw new InvalidOperationException("No active changeset to rollback.");
        
        var rowContainer = table._rowContainer;
        var uniqueIndexes = table._indexes;

        //rollback changes to rowContainer using the entries in the undo log
        var entriesToRemove = _entries.Count - _changeSets.Pop();
        for(var i=0; i<entriesToRemove; i++)
        {
            var (type, row) = _entries.Pop();

            switch (type)
            {
                case LogType.Add:
                    foreach (var index in uniqueIndexes)
                        index.Remove(in row);
                    rowContainer.Remove(in row);
                    break;
                case LogType.Update:
                    var existingRow = rowContainer.Get(row);
                    foreach (var index in uniqueIndexes)
                        index.Update(in existingRow, in row);
                    rowContainer.Set(ref row);
                    _updateEntries.Pop();
                    break;
                case LogType.Remove:
                    foreach (var index in uniqueIndexes)
                        index.Add(in row);
                    rowContainer.Add(ref row);
                    break;
                case LogType.KeyGeneratorReset:
                    // Reset the key generator to the previous value
                    var keyGenerator = table._keyGenerator;
                    if (keyGenerator != null)
                    {
                        keyGenerator.Reset(row.id);
                    }
                    break;
            }
        }

        if (_entries.Count == 0 && _updateEntries.Count > 0)
        {
            throw new Exception("The undo entries and update log are out of sync. This is a bug.");
        }

        if (_changeSets.Count == 0)
            Exception = null;
    }

    public void RegisterUpdate(Row<T> existingRow, Row<T> newRow)
    {
        Register(LogType.Update, existingRow);
        _updateEntries.Push(newRow);
    }

    public void RegisterAdd(Row<T> row)
    {
        Register(LogType.Add, row);
    }

    public void RegisterRemove(Row<T> row)
    {
        Register(LogType.Remove, row);
    }

    public void RegisterBeginChangeSet()
    {
        _changeSets.Push(_entries.Count);
    }

    public void RegisterKeyGeneratorReset(int keyGeneratorCurrentKey)
    {
        Register(LogType.KeyGeneratorReset, new Row<T> { id = keyGeneratorCurrentKey });
    }
}