using System;
using System.Collections.Generic;

namespace IntegrityTables;

public delegate bool JoinConditionFunc<T1, T2>(in Row<T1> row1, in Row<T2> row2) where T1 : IEquatable<T1> where T2 : IEquatable<T2>;

public struct JoinEnumerator<T1, T2> where T1 : struct, IEquatable<T1> where T2 : struct, IEquatable<T2>
{
    private int _index1;
    private int _index2;
    private readonly IReadableTable<T1> _rowContainer1;
    private readonly IReadableTable<T2> _rowContainer2;
    private readonly JoinConditionFunc<T1, T2> _condition;

    public (Row<T1>, Row<T2>) Current { get; private set; }

    public JoinEnumerator(IReadableTable<T1> table1, IReadableTable<T2> table2, JoinConditionFunc<T1, T2> condition)
    {
        _rowContainer1 = table1;
        _rowContainer2 = table2;
        _condition = condition;
        _index1 = -1;
        _index2 = -1;
        Current = default;
    }

    public bool MoveNext()
    {
        while (true)
        {
            if (_index1 >= _rowContainer1.Count)
                return false;

            if (_index1 < 0 || _index2 >= _rowContainer2.Count - 1)
            {
                _index1++;
                _index2 = 0;
            }
            else
            {
                _index2++;
            }

            if (_index1 >= _rowContainer1.Count)
                return false;
            if (_index2 >= _rowContainer2.Count)
                return false;

            var row1 = _rowContainer1[_index1];
            var row2 = _rowContainer2[_index2];

            if (_condition(in row1, in row2))
            {
                Current = (row1, row2);
                return true;
            }
        }
    }

    public void Reset()
    {
        _index1 = -1;
        _index2 = -1;
        Current = default;
    }

    public JoinEnumerator<T1, T2> GetEnumerator()
    {
        return this;
    }

    public (Row<T1>, Row<T2>)[] ToArray()
    {
        return ToList().ToArray();
    }

    public List<(Row<T1>, Row<T2>)> ToList()
    {
        var list = new List<(Row<T1>, Row<T2>)>();
        while (MoveNext()) list.Add(Current);
        return list;
    }
}