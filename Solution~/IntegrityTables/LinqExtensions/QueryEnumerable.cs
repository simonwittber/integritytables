using System;
using System.Collections.Generic;

namespace IntegrityTables;

public readonly struct QueryEnumerable<T, TOut> where T : struct, IEquatable<T>
{
    internal readonly Table<T> table;
    internal readonly Func<Row<T>, bool>? condition;
    internal readonly Func<Row<T>, TOut> selector;

    public QueryEnumerable(Table<T> table, Func<Row<T>, bool>? condition, Func<Row<T>, TOut> selector)
    {
        this.table = table;
        this.condition = condition;
        this.selector = selector;
    }

    public Enumerator GetEnumerator() => new Enumerator(table, condition, selector);

    public struct Enumerator
    {
        private readonly Table<T> _table;
        private readonly Func<Row<T>, bool>? _condition;
        private readonly Func<Row<T>, TOut> _selector;
        private int _index;

        internal Enumerator(Table<T> table, Func<Row<T>, bool>? condition, Func<Row<T>, TOut> selector)
        {
            _table = table;
            _condition = condition;
            _selector = selector;
            _index = -1;
        }

        public bool MoveNext()
        {
            while (++_index < _table.Count)
            {
                var row = _table[_index];
                if (_condition == null || _condition(row))
                    return true;
            }

            return false;
        }

        public TOut Current => _selector(_table[_index]);
    }

    public List<TOut> ToList()
    {
        var list = new List<TOut>();
        var enumerator = GetEnumerator();
        while (enumerator.MoveNext())
        {
            list.Add(enumerator.Current);
        }

        return list;
    }
}