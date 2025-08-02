using System;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    /// <summary>
    /// Sorts the table *in place* by an arbitrary Comparison&lt;T&gt;.
    /// </summary>
    public void Sort(Comparison<Row<T>> comparison)
    {
        using(_lock.WriteScope())
        {
            // snapshot
            var rows = ToArray();
            // sort by payload T
            Array.Sort(rows, (r1, r2) => comparison(r1, r2));

            // rebuild container & indexes
            _rowContainer.Clear(rows.Length);
            foreach (var idx in _indexes) idx.Clear();

            for (int i = 0; i < rows.Length; i++)
            {
                ref var row = ref rows[i];
                row._index = i; // patch the new slot
                _rowContainer.Add(ref row); // insert into container
                foreach (var idx in _indexes) // rebuild unique indexes
                    idx.Add(in row);
            }
        }
        
    }

    public void SortBy<TKey1, TKey2>(
        Func<Row<T>, TKey1> key1, bool desc1,
        Func<Row<T>, TKey2> key2, bool desc2)
        where TKey1 : IComparable<TKey1>
        where TKey2 : IComparable<TKey2>
    {
        Sort((a, b) =>
        {
            var cmp1 = key1(a).CompareTo(key1(b));
            if (desc1) cmp1 = -cmp1;
            if (cmp1 != 0) return cmp1;

            var cmp2 = key2(a).CompareTo(key2(b));
            return desc2 ? -cmp2 : cmp2;
        });
    }

    public void SortBy<TKey1, TKey2, TKey3>(
        Func<Row<T>, TKey1> key1, bool desc1,
        Func<Row<T>, TKey2> key2, bool desc2,
        Func<Row<T>, TKey3> key3, bool desc3)
        where TKey1 : IComparable<TKey1>
        where TKey2 : IComparable<TKey2>
        where TKey3 : IComparable<TKey3>
    {
        Sort((a, b) =>
        {
            var cmp1 = key1(a).CompareTo(key1(b));
            if (desc1) cmp1 = -cmp1;
            if (cmp1 != 0) return cmp1;

            var cmp2 = key2(a).CompareTo(key2(b));
            if (desc2) cmp2 = -cmp2;

            if (cmp2 != 0) return cmp2;
            var cmp3 = key3(a).CompareTo(key3(b));
            return desc3 ? -cmp3 : cmp3;
        });
    }
}