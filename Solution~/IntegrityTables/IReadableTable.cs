using System;
using System.Collections.Generic;

namespace IntegrityTables;

public interface IReadableTable<T> : ITable where T : struct, IEquatable<T>
{
    bool Exists(RowConditionFunc<T> func);
    Row<T> Get(int id);
    bool TryGet(int id, out Row<T> row);
    QueryEnumerator<T> Query(RowConditionFunc<T>? func = null);
    Row<T> GetOne(Func<T, bool> func);
    bool TryGetOne(Func<T, bool> func, out Row<T> row);
    RowIdEnumerator<T> GetEnumerator();
    Row<T>[] ToArray();
    List<Row<T>> ToList();
    int ChangeSetCount { get; }
    bool HasException { get; }
    void Sort(Comparison<Row<T>> comparison);
    JoinEnumerator<T, T2> Joined<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>;
    ManyToManyEnumerator<T, T2> ManyToManyJoin<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>;
    new Row<T> this[int index] { get; }
}