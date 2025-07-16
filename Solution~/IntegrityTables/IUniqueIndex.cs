using System;

namespace IntegrityTables;

public interface IUniqueIndex<T> where T : struct, IEquatable<T>
{
    public string Name { get; }
    void Add(in Row<T> row);
    void Remove(in Row<T> row);
    void Update(in Row<T> oldRow, in Row<T> newRow);
    void Clear();
}