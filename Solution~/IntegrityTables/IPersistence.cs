using System;

namespace IntegrityTables;

public interface IPersistence {
    void SaveTable<T>(ITable<T> table) where T : struct, IEquatable<T>;
    void LoadTable<T>(ITable<T> table) where T : struct, IEquatable<T>;

}