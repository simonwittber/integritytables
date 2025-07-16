using System;

namespace IntegrityTables;

public partial class Table<T>
{
    public RowIdEnumerator<T> GetEnumerator()
    {
        lock (_sync)
            return new RowIdEnumerator<T>(this);
    }
    
    public RowEnumerator<T> Rows()
    {
        lock (_sync)
            return new RowEnumerator<T>(this);
    }

    public JoinEnumerator<T, T2> Joined<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>
    {
        lock (_sync)
            return new JoinEnumerator<T, T2>(this, otherTable, condition);
    }

    public ManyToManyEnumerator<T, T2> ManyToManyJoin<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>
    {
        lock (_sync)
            return new ManyToManyEnumerator<T, T2>(this, otherTable, condition);
    }

    public QueryEnumerator<T> Query(RowConditionFunc<T>? func = null)
    {
        lock (_sync)
            return new QueryEnumerator<T>(this, func);
    }

    public QueryEnumerator<T> Query(DataConditionFunc<T> func)
    {
        lock (_sync)
            return new QueryEnumerator<T>(this, ((in Row<T> row) => func(row.data)));
    }
}