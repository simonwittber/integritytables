using System;

namespace IntegrityTables;

public partial class Table<T>
{
    public RowIdEnumerator<T> GetEnumerator()
    {
        using(_lock.ReadScope())
        {
            return new RowIdEnumerator<T>(this);
        }
        
    }

    public RowEnumerator<T> Rows()
    {
        using(_lock.ReadScope())
        {
            return new RowEnumerator<T>(this);
        }
        
    }

    public JoinEnumerator<T, T2> Joined<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>
    {
        using(_lock.ReadScope())
        {
            return new JoinEnumerator<T, T2>(this, otherTable, condition);
        }
        
    }

    public ManyToManyEnumerator<T, T2> ManyToManyJoin<T2>(IReadableTable<T2> otherTable, JoinConditionFunc<T, T2> condition) where T2 : struct, IEquatable<T2>
    {
        using(_lock.ReadScope())
        {
            return new ManyToManyEnumerator<T, T2>(this, otherTable, condition);
        }
        
    }

    public QueryEnumerator<T> Query(RowConditionFunc<T>? func = null)
    {
        using(_lock.ReadScope())
        {
            return new QueryEnumerator<T>(this, func);
        }
        
    }

    public QueryEnumerator<T> Query(DataConditionFunc<T> func)
    {
        using(_lock.ReadScope())
        {
            return new QueryEnumerator<T>(this, ((in Row<T> row) => func(row.data)));
        }
        
    }
}