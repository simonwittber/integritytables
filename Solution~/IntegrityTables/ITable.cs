using System;

namespace IntegrityTables;

public interface ITable
{
    string Name { get; }
    int Count { get; }
    System.Type RowType { get; }
    System.Type ListType { get; }
    System.Type DataType { get; }
    void BeginChangeSet();
    void CommitChangeSet();
    void RollbackChangeSet();
    bool ContainsKey(int key);
    RowModifiedActionList OnRowModified { get; set; }
    ITableMetadata? Metadata { get; }
    RowObjectAdapter this[int key] { get; }
    public Exception? Exception { get; }
    void Update(RowObjectAdapter adapter);
    bool TryUpdate(RowObjectAdapter adapter);
    void Add(RowObjectAdapter adapter);
    bool TryAdd(RowObjectAdapter adapter);
    RowObjectAdapter CreateEmptyRow();
    void Remove(int id, CascadeOperation cascadeOperation = CascadeOperation.None);
    bool TryRemove(int id, CascadeOperation cascadeOperation = CascadeOperation.None);
    void Clear(CascadeOperation cascadeOperation = CascadeOperation.None);
}

public interface ITable<T> : IReadableTable<T>, IWriteableTable<T> where T : struct, IEquatable<T>
{
    void AddConstraint(RowConditionFunc<T> func, string name);
    IDisposable AddObserver(Row<T> row, Action<Row<T>>? onUpdated);
    void RemoveObserver(Row<T> row, Action<Row<T>>? onUpdated);
}
