using System;

namespace IntegrityTables;

public class DisposableObserver<T> : IDisposable where T : struct, IEquatable<T>
{
    public void Dispose()
    {
        Table?.RemoveObserver(Row, OnUpdated);
    }

    public Row<T> Row;
    public /*required*/ Action<Row<T>>? OnUpdated;
    public /*required*/ Table<T>? Table;
}