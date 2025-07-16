using System;
using System.Collections.Generic;

namespace IntegrityTables;

public interface IWriteableTable<T> where T : struct, IEquatable<T>
{
    Row<T> Add(T data);
    void Add(ref Row<T> row);
    bool TryAdd(ref Row<T> row, bool enableTriggers=true, bool enableConstraints=true);
    bool TryRemove(in Row<T> row, CascadeOperation cascadeOperation = CascadeOperation.None);
    void Remove(in Row<T> row, CascadeOperation cascadeOperation = CascadeOperation.None);
    // void Remove(int id, CascadeOperation cascadeOperation = CascadeOperation.None);
    // bool TryRemove(int id, CascadeOperation cascadeOperation = CascadeOperation.None);
    bool TryUpdate(ref Row<T> row);
    void Update(ref Row<T> row);
    void Load(IList<Row<T>> rows);
    void Clear(CascadeOperation cascadeOperation = CascadeOperation.None);
    
    // These callbacks allow an external system to validate the data before adding or updating.
    // This is used in the database source generator to ensure validation across referenced tables.
    Action<T,T>? ValidateForUpdate { get; set; }
    Action<T>? ValidateForAdd { get; set; }
    Action<int, CascadeOperation>? ExecuteCascadingRemove { get; set; }

}