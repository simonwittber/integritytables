using System;
using System.Collections.Generic;

namespace IntegrityTables;

public readonly struct QueryJoinEnumerable<TL, TR, TResult> where TL : struct, IEquatable<TL> where TR : struct, IEquatable<TR>
{
    internal readonly Table<TL> LeftTable;
    internal readonly Table<TR> RightTable;
    internal readonly Func<Row<TL>, Row<TR>, bool> Condition;
    internal readonly Func<Row<TL>, Row<TR>, TResult> Selector;

    public QueryJoinEnumerable(Table<TL> left, Table<TR> right, Func<Row<TL>, Row<TR>, bool> condition, Func<Row<TL>, Row<TR>, TResult> selector)
    {
        LeftTable = left;
        RightTable = right;
        Condition = condition;
        Selector = selector;
    }

    public Enumerator GetEnumerator() => new Enumerator(LeftTable, RightTable, Condition, Selector);

    public struct Enumerator
    {
        private readonly Table<TL> _leftTable;
        private readonly Table<TR> _rightTable;
        private readonly Func<Row<TL>, Row<TR>, bool> _condition;
        private readonly Func<Row<TL>, Row<TR>, TResult> _selector;
        private int _leftIndex, _rightIndex;

        internal Enumerator(Table<TL> leftTable, Table<TR> rightTable, Func<Row<TL>, Row<TR>, bool> condition, Func<Row<TL>, Row<TR>, TResult> selector)
        {
            _leftTable = leftTable;
            _rightTable = rightTable;
            _condition = condition;
            _selector = selector;
            _leftIndex = 0;
            _rightIndex = -1;
        }

        public bool MoveNext()
        {
            while (_leftIndex < _leftTable.Count)
            {
                while (++_rightIndex < _rightTable.Count)
                {
                    var lRow = _leftTable[_leftIndex];
                    var rRow = _rightTable[_rightIndex];
                    if (_condition(lRow, rRow))
                        return true;
                }

                _leftIndex++;
                _rightIndex = -1;
            }

            return false;
        }

        public TResult Current => _selector(_leftTable[_leftIndex], _rightTable[_rightIndex]);
    }

    public List<TResult> ToList()
    {
        var list = new List<TResult>();
        var e = GetEnumerator();
        while (e.MoveNext())
            list.Add(e.Current);
        return list;
    }
}