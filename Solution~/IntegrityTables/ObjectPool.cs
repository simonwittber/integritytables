using System.Collections.Generic;

namespace IntegrityTables;

public static class ObjectPool<T> where T : class, new()
{
    private static readonly Stack<T> _pool = new Stack<T>();

    public static T Get()
    {
        lock(_pool)
            return _pool.Count > 0 ? _pool.Pop() : new T();
    }

    public static void Return(T item)
    {
        lock(_pool)
            _pool.Push(item);
    }
}