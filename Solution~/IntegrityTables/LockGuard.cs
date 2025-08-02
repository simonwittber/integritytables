using System;
using System.Threading;

namespace IntegrityTables;

public readonly struct LockGuard : IDisposable
{
    private readonly ReaderWriterLockSlim _lock;
    private readonly bool _isWrite;

    internal LockGuard(ReaderWriterLockSlim _lock, bool isWrite)
    {
        this._lock = _lock;
        _isWrite = isWrite;
        if (isWrite)
            this._lock.EnterWriteLock();
        else
            this._lock.EnterReadLock();
    }

    public void Dispose()
    {
        if (_isWrite)
            _lock.ExitWriteLock();
        else
            _lock.ExitReadLock();
    }
}

public static class ReaderWriterLockSlimExtensions
{
    public static LockGuard ReadScope(this ReaderWriterLockSlim _lock) => new LockGuard(_lock, isWrite: false);

    public static LockGuard WriteScope(this ReaderWriterLockSlim _lock) => new LockGuard(_lock, isWrite: true);
}