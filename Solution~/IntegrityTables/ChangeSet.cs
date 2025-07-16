using System;

namespace IntegrityTables;

public class ChangeSet : IDisposable
{
    private readonly ITable[] _tables;
    private bool _completed;

    public ChangeSet(params ITable[] tables)
    {
        _tables = tables;
        _completed = false;
        foreach (var t in _tables) t.BeginChangeSet();
    }

    public void Dispose()
    {
        if (!_completed)
        {
            foreach (var t in _tables)
            {
                if (t.Exception is not null)
                {
                    var ex = t.Exception;
                    Rollback();
                    throw ex;
                }
            }

            Warnings.Warn("ChangeSet not committed or explicitly rolled back. Rolling back changes automatically.");
            Rollback();
        }
    }

    public void Commit()
    {
        if (_completed) return;
        foreach (var t in _tables) t.CommitChangeSet();
        _completed = true;
    }

    public void Rollback()
    {
        if (_completed) return;
        foreach (var t in _tables) t.RollbackChangeSet();
        _completed = true;
    }
}