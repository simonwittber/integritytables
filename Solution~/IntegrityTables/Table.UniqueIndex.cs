using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T>
{
    private readonly Dictionary<string, IUniqueIndex<T>> _indexMap = new();

    public void AddUniqueIndex<TU>(string name, UniqueIndex<T, TU>.GetKeyDelegate getKeyFunc) where TU : struct
    {
        lock (_sync)
        {
            var index = new UniqueIndex<T, TU>(this, name, getKeyFunc, _capacity);
            Array.Resize(ref _indexes, _indexes.Length + 1);
            _indexes[^1] = index;
            for (var i = 0; i < _rowContainer.Count; i++)
            {
                index.Add(_rowContainer[i]);
            }

            _indexMap[name] = index;
        }
    }

    internal IUniqueIndex<T>[] _indexes = [];
}