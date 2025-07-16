// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;
//
// namespace IntegrityTables.Linq;
//
// public readonly struct TableQuery<T>(Table<T> table) where T : struct
// {
//     public TableEnumerator<T> GetEnumerator() => table.GetEnumerator();
// }
//
// public readonly struct WhereQuery<T>(TableQuery<T> source, RowConditionFunc<T> predicate) where T : struct
// {
//     public WhereEnumerator<T> GetEnumerator() => new WhereEnumerator<T>(source.GetEnumerator(), predicate);
// }
//
// public ref struct WhereEnumerator<T>(TableEnumerator<T> inner, RowConditionFunc<T> predicate)
//     where T : struct
// {
//     private TableEnumerator<T> _innerEnumerator = inner;
//
//     public bool MoveNext()
//     {
//         // Keep calling MoveNext on the inner enumerator until predicate matches or we run out
//         while (_innerEnumerator.MoveNext())
//         {
//             if (predicate(in _innerEnumerator.Current))
//                 return true;
//         }
//
//         return false;
//     }
//
//     public ref readonly Row<T> Current => ref _innerEnumerator.Current;
// }
//
// public delegate TResult RowSelectorFunc<TSource, TResult>(Row<TSource> row);
//
// public readonly struct SelectQuery<TSource, TResult> where TSource : struct where TResult : struct
// {
//     // We allow selecting from *any* previous query that yields Row<TSource>,
//     // but for simplicity let’s assume the source is a WhereQuery<TSource>.
//     private readonly WhereQuery<TSource> _source; 
//     private readonly RowSelectorFunc<TSource, TResult> _selector;
//
//     public SelectQuery(in WhereQuery<TSource> source, RowSelectorFunc<TSource, TResult> selector)
//     {
//         _source   = source;
//         _selector = selector;
//     }
//
//     public SelectEnumerator<TSource, TResult> GetEnumerator()
//         => new SelectEnumerator<TSource, TResult>(_source.GetEnumerator(), _selector);
// }
//
// /// <summary>
// /// The enumerator for SelectQuery. It calls MoveNext() on the underlying WhereEnumerator,
// /// then applies the selector function to produce a TResult.
// /// </summary>
// public ref struct SelectEnumerator<TSource, TResult>
//     where TSource : struct
//     where TResult : struct
// {
//     private WhereEnumerator<TSource> _inner;
//     private readonly RowSelectorFunc<TSource, TResult> _selector;
//
//     public SelectEnumerator(WhereEnumerator<TSource> inner, RowSelectorFunc<TSource, TResult> selector)
//     {
//         _inner    = inner;
//         _selector = selector;
//     }
//
//     public bool MoveNext()
//     {
//         return _inner.MoveNext();
//     }
//
//     public TResult Current => _selector(in _inner.Current);
// }
//
// public static class TableQueryExtensions
// {
//     /// <summary>
//     /// Extension method so you can write:
//     ///    from t in new TableQuery<T>(table)
//     ///    where t.data.SomeField > 0
//     ///    select t;
//     /// </summary>
//     public static WhereQuery<T> Where<T>(this TableQuery<T> source, RowConditionFunc<T> predicate) where T : struct
//     {
//         return new WhereQuery<T>(source, predicate);
//     }
//     
//     /// <summary>
//     /// Allow “.Select(...)” directly on a TableQuery<TSource> (no Where clause).
//     /// We simply wrap it in a trivial WhereQuery that always returns true.
//     /// </summary>
//     public static SelectQuery<TSource, TResult> Select<TSource, TResult>(
//         this in TableQuery<TSource> source,
//         RowSelectorFunc<TSource, TResult> selector
//     )
//     where TSource : struct
//         where TResult : struct
//     {
//         // Wrap in a “predicate = always true” so that 
//         // WhereEnumerator just yields every row
//         var alwaysTrue = new WhereQuery<TSource>(source, (in Row<TSource> _) => true);
//         return new SelectQuery<TSource, TResult>(alwaysTrue, selector);
//     }
//
//     /// <summary>
//     /// Allow “.Select(...)” after a WhereQuery<TSource> as well.
//     /// </summary>
//     public static SelectQuery<TSource, TResult> Select<TSource, TResult>(
//         this in WhereQuery<TSource> source,
//         RowSelectorFunc<TSource, TResult> selector
//     )
//     where TSource : struct
//         where TResult : struct
//     {
//         return new SelectQuery<TSource, TResult>(source, selector);
//     }
// }