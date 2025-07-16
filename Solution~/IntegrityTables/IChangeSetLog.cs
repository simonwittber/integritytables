using System;

namespace IntegrityTables;

public interface IChangeSetLog<T> where T : struct, IEquatable<T>
{
    bool HasException { get; }
    bool IsEmpty { get; }
    int Count { get; }
    Exception? Exception { get; set; }
    bool IsActive { get; }
    void Discard();
    void Rollback(Table<T> table);
    void RegisterUpdate(Row<T> existingRow, Row<T> newRow);
    void RegisterAdd(Row<T> row);
    void RegisterRemove(Row<T> row);
    void RegisterBeginChangeSet();
    void RegisterKeyGeneratorReset(int keyGeneratorCurrentKey);
}