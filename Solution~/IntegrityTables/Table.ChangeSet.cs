using System;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    private IChangeSetLog<T> _changeSetLog;
    private bool _isInChangeSet;

    private void CheckChangeSetState()
    {
        if (_isInChangeSet && _changeSetLog.HasException)
            throw new InvalidOperationException("Transaction has failed");
    }

    public void BeginChangeSet()
    {
        using(_lock.WriteScope())
        {
            if (_changeSetLog.HasException)
                throw new InvalidOperationException("Cannot commit, the changeset has an exception. Call RollbackChangeSet.");
            _changeSetLog.RegisterBeginChangeSet();
            _isInChangeSet = _changeSetLog.IsActive;
        }
    }

    public void CommitChangeSet()
    {
        using(_lock.WriteScope())
        {
            if (!_isInChangeSet)
                throw new InvalidOperationException("Cannot commit, no changeset is in progress. Call BeginChangeSet first.");
            if (_changeSetLog.HasException)
                throw new InvalidOperationException("Cannot commit, the changeset has an exception. Call RollbackChangeSet.");
            _changeSetLog.Discard();
            _isInChangeSet = _changeSetLog.IsActive;
        }
        
    }

    public void RollbackChangeSet()
    {
        using(_lock.WriteScope())
        {
            if (!_isInChangeSet)
                throw new InvalidOperationException("Cannot rollback, no changeset is in progress. Call BeginChangeSet first.");
            _changeSetLog.Rollback(this);
            _isInChangeSet = _changeSetLog.IsActive;
        }
        
    }
}