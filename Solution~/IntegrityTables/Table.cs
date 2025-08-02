using System;
using System.Collections.Generic;
using System.Threading;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    internal IRowContainer<T> _rowContainer;
    internal TableKeyGenerator _keyGenerator;
    private readonly int _capacity;
    private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);


    private void RaiseException(Exception exception)
    {
        if (_isInChangeSet) _changeSetLog.Exception = exception;
        throw exception;
    }

    /// <summary>
    /// Raise an InvalidOperationException(message) and invalidate the changeset log.
    /// </summary>
    public void RaiseException(string message)
    {
        RaiseException(new InvalidOperationException(message));
    }

    internal void ResetKeyGenerator()
    {
        var maxKey = 0;
        using (_lock.WriteScope())
        {
            for (var i = 0; i < _rowContainer.Count; i++)
            {
                var row = _rowContainer[i];
                if (row.id > maxKey)
                    maxKey = row.id;
            }

            if (_isInChangeSet) _changeSetLog.RegisterKeyGeneratorReset(_keyGenerator.CurrentKey);
            _keyGenerator.Reset(maxKey);
        }
    }
}