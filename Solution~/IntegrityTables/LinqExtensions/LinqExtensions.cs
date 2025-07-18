using System;

namespace IntegrityTables.LinqExtensions;

public static class IntegrityTablesLinqExtensions
{
    // from i in table select i.X
    public static QueryEnumerable<T, TOut> Select<T, TOut>(this Table<T> table, Func<Row<T>, TOut> selector) where T : struct, IEquatable<T>
    {
        return new QueryEnumerable<T, TOut>(table, condition: null, selector);
    }

    // from i in table where i.X
    public static QueryEnumerable<T, Row<T>> Where<T>(this Table<T> table, Func<Row<T>, bool> predicate) where T : struct, IEquatable<T>
    {
        return new QueryEnumerable<T, Row<T>>(table, predicate, selector: r => r);
    }

    // from i in table where i.X select i.Y
    public static QueryEnumerable<T, TNew> Select<T, TOld, TNew>(this QueryEnumerable<T, TOld> source, Func<TOld, TNew> selector) where T : struct, IEquatable<T>
    {
        // capture the old predicate & selector
        var pred = source.condition;
        var oldSel = source.selector;

        // build a combined selector: Row<T> → TOld → TNew
        TNew NewSel(Row<T> row) => selector(oldSel(row));

        return new QueryEnumerable<T, TNew>(source.table, pred, NewSel);
    }

    /// from l in leftTable
    /// join r in rightTable on predicate(l,r)
    /// select (l,r)
    public static QueryJoinEnumerable<TLeft, TRight, TResult> Join<TLeft, TRight, TKey, TResult>
        (this Table<TLeft> left, Table<TRight> right, Func<Row<TLeft>, TKey> leftKeySelector, Func<Row<TRight>, TKey> rightKeySelector, Func<Row<TLeft>, Row<TRight>, TResult> resultSelector) where TLeft : struct, IEquatable<TLeft> where TRight : struct, IEquatable<TRight>
    {
        return new QueryJoinEnumerable<TLeft, TRight, TResult>(
            left, 
            right, 
            (l, r) => leftKeySelector(l).Equals(rightKeySelector(r)), 
            (l, r) => resultSelector(l, r));
    }

    /// <summary>
    /// filter an existing join by an additional condition
    /// </summary>
    public static QueryJoinEnumerable<TLeft, TRight, TResult> Where<TLeft, TRight, TResult>
        (this QueryJoinEnumerable<TLeft, TRight, TResult> source, Func<Row<TLeft>, Row<TRight>, bool> predicate) where TLeft : struct, IEquatable<TLeft> where TRight : struct, IEquatable<TRight>
    {
        // compose old + new predicate
        Func<Row<TLeft>, Row<TRight>, bool> composedCondition = (l, r) => source.Condition(l, r) && predicate(l, r);

        return new QueryJoinEnumerable<TLeft, TRight, TResult>(source.LeftTable, source.RightTable, composedCondition, source.Selector);
    }

    /// <summary>
    /// project the result of a join into a new shape
    /// </summary>
    public static QueryJoinEnumerable<TL, TR, TNew> Select<TL, TR, TOld, TNew>(this QueryJoinEnumerable<TL, TR, TOld> source, Func<TOld, TNew> projector) where TL : struct, IEquatable<TL> where TR : struct, IEquatable<TR>
    {
        // compose selector: (Row<TL>,Row<TR>) -> TOld -> TNew
        Func<Row<TL>, Row<TR>, TNew> compSel = (l, r) => projector(source.Selector(l, r));

        return new QueryJoinEnumerable<TL, TR, TNew>(source.LeftTable, source.RightTable, source.Condition, compSel);
    }
    
}