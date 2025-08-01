using System;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrityTables;

public sealed class Context<T> : IDisposable, IAsyncDisposable where T : class
{
    private static readonly AsyncLocal<T?> _current = new AsyncLocal<T?>();
    
    public static T Current
    {
        get
        {
            if(_current.Value == null)
                throw new InvalidOperationException("No ambient context is currently set.");
            return _current.Value;
        }
    }

    private readonly T? _previous;

    public Context(T context)
    {
        if(context == null) 
            throw new ArgumentNullException(nameof(context), "context cannot be null.");
        _previous      = _current?.Value;
        _current!.Value = context;
    }

    // synchronous dispose
    public void Dispose()
    {
        _current.Value = _previous;
    }

    // async dispose so you can 'await using'
    public ValueTask DisposeAsync()
    {
        _current.Value = _previous;
        return default;
    }
}