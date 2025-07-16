using System.Collections.Generic;

namespace IntegrityTables;

public class InsertionSortedList<TK, TV>
{
    private readonly List<(TK sortKey, TV value)> _items = new();
    private readonly IComparer<(TK, TV)> _tupleComparer;

    public InsertionSortedList(IComparer<TK>? keyComparer = null)
    {
        var keyComparer1 = keyComparer ?? Comparer<TK>.Default;
        _tupleComparer = Comparer<(TK sortKey, TV value)>.Create(
            (a, b) => keyComparer1.Compare(a.sortKey, b.sortKey)
        );
    }

    public void Add(TK sortKey, TV value)
    {
        var item = (sortKey, value);
        int idx = _items.BinarySearch(item, _tupleComparer);
        if (idx < 0) idx = ~idx;
        _items.Insert(idx, item);
    }

    public bool RemoveByValue(TV value)
    {
        int idx = _items.FindIndex(item =>
            EqualityComparer<TV>.Default.Equals(item.value, value)
        );
        if (idx < 0) return false;
        _items.RemoveAt(idx);
        return true;
    }

    public (TK sortKey, TV value) this[int index] => _items[index];
    public int Count => _items.Count;
    
    public List<(TK,TV)>.Enumerator GetEnumerator() => _items.GetEnumerator();

}