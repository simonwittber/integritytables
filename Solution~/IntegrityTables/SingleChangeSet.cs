using System;

namespace IntegrityTables;

internal class SingleChangeSet : IDisposable
{
    private readonly ITable _table;
    private bool _completed;

    public SingleChangeSet(ITable table)
    {
        _table = table;
        _completed = false;
        _table.BeginChangeSet();
    }

    public void Dispose()
    {
        if (!_completed)
        {
            if (_table.Exception is not null)
            {
                var ex = _table.Exception;
                Rollback();
                throw ex;
            }

            Warnings.Warn("ChangeSet not committed or explicitly rolled back. Rolling back changes automatically.");
            Rollback();
        }
    }

    public void Commit()
    {
        if (_completed) return;
        _table.CommitChangeSet();
        _completed = true;
    }

    public void Rollback()
    {
        if (_completed) return;
        _table.RollbackChangeSet();
        _completed = true;
    }
}