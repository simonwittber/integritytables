using System.Runtime.CompilerServices;
using System;

[assembly: InternalsVisibleTo("Tests")]


namespace IntegrityTables;

internal class RowContainer<T> : IRowContainer<T> where T : struct, IEquatable<T>
{
    private Row<T>[] _rows = new Row<T>[4096];
    public int Count { get; private set; }
    public int Version => _version;
    private int _version = 0;

    public event Action<int, TableOperation>? OnRowModified;

    private IIntegerMap _idToIndex = new IdMap();

    private void ResizeIfNeeded()
    {
        if (_rows.Length > Count) return;
        var newSize = _rows.Length == 0 ? 4 : _rows.Length * 2;
        Array.Resize(ref _rows, newSize);
    }

    public void Remove(in Row<T> row)
    {
        var lastIndex = Count - 1;
        var removedRowIndex = GetIndexForId(row.id);
        _version++;
        _idToIndex.Remove(row.id);
        for (int i = removedRowIndex; i < lastIndex; i++)
        {
            var moved = _rows[i + 1];
            moved._index = i;
            _rows[i] = moved;
            _idToIndex[moved.id] = i;
            OnRowModified?.Invoke(i, TableOperation.Update);
        }

        // clear the last index to default values
        _rows[lastIndex] = default;
        // reduce count, effectively removing the row
        Count--;
        OnRowModified?.Invoke(lastIndex, TableOperation.Remove);
    }

    public bool ContainsKey(int id) => _idToIndex.ContainsKey(id);

    public bool TryGetIndexForId(int id, out int index) => _idToIndex.TryGetValue(id, out index);

    public bool TryGetIdForIndex(int index, out int id)
    {
        if(index < 0 || index >= Count) 
        {
            id = default;
            return false;
        }
        id = _rows[index].id;
        return true;
    }

    public int GetIndexForId(int id) => _idToIndex[id];

    public Row<T> Get(Row<T> row)
    {
        return _rows[GetIndexForId(row.id)];
    }

    public void Set(ref Row<T> row)
    {
        row._index = GetIndexForId(row.id);
        _rows[row._index] = row;
        OnRowModified?.Invoke(row._index, TableOperation.Update);
    }

    public void Add(ref Row<T> row)
    {
        ResizeIfNeeded();
        row._index = Count;
        _rows[row._index] = row;
        _idToIndex[row.id] = row._index;
        Count++;
        _version++;
        OnRowModified?.Invoke(row._index, TableOperation.Add);
    }

    public Row<T> this[int index] => _rows[index];

    public void Clear(int capacity = 10)
    {
        _rows = new Row<T>[capacity];
        _idToIndex.Clear();
        _version++;
        Count = 0;
    }
}