using System;

namespace IntegrityTables;

public interface IRowContainer<T> where T : struct, IEquatable<T>
{
    int Count { get; }
    int Version { get; }
    event Action<int, TableOperation> OnRowModified;
    void Remove(in Row<T> row);
    bool ContainsKey(int id);
    bool TryGetIndexForId(int id, out int index);
    bool TryGetIdForIndex(int index, out int id);
    int GetIndexForId(int id);
    Row<T> Get(Row<T> row);
    void Set(ref Row<T> row);
    void Add(ref Row<T> row);
    Row<T> this[int index] { get; }
    void Clear(int capacity = 10);
}