using System;
using System.Collections;
using System.Collections.Generic;

namespace IntegrityTables;

[Serializable]
public class ObservableList<T> : IListChangeNotifier<T>, IEnumerable<T>, IList<T>
{
    public List<T> items;
    
    public event Action<int, T>? ItemAdded;
    public event Action<int, T>? ItemRemoved;
    public event Action<int, T>? ItemUpdated;
    public event Action? Cleared;

    public int Count => items.Count;
    public bool IsReadOnly => false;
    
    public ObservableList(int capacity = 37)
    {
        items = new List<T>(capacity);
    }

    public void Add(T item)
    {
        items.Add(item);
        ItemAdded?.Invoke(items.Count - 1, item);
    }

    public int IndexOf(T item) => items.IndexOf(item);

    public void Insert(int index, T item)
    {
        items.Insert(index, item);
        ItemAdded?.Invoke(index, item);
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= items.Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        var item = items[index];
        items.RemoveAt(index);
        ItemRemoved?.Invoke(index, item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        if (array == null) throw new ArgumentNullException(nameof(array));
        if (arrayIndex < 0 || arrayIndex + Count > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex));
        items.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        var index = items.IndexOf(item);
        if (index < 0)
            return false;
        RemoveAt(index);
        return true;
    }
    
    public void Clear()
    {
        items.Clear();
        Cleared?.Invoke();
    }

    public T this[int i]
    {
        get => items[i];
        set
        {
            items[i] = value;
            ItemUpdated?.Invoke(i, value);
        }
    }

    public void AddRange(IEnumerable<T> newItems)
    {
        foreach (var item in newItems)
        {
            Add(item);
        }
    }
    
    public bool Contains(T item) => items.Contains(item);
    
    public List<T> ToList() => new(items);
    
    // pattern-based enumerator (no allocation)
    public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();

    // explicit interface impls
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

}