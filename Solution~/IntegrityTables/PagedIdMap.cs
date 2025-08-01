using System;
using System.Collections.Generic;
using System.Linq;

namespace IntegrityTables;

// PagedMap is a memory-efficient map for large ranges of integer keys.
// IdMap is preferred for primary key to index mapping, as we know that the keys start from 0 and are dense.
// PagedMap is designed for cases where the keys are sparse or large, and we want to minimize memory usage.

public class PagedIdMap : IIdMap
{
    private const int PageBits = 10;          // 1024 entries per page
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;

    // outer list of pages; pages[p] is null until first use
    private readonly List<int[]> _pages = new List<int[]>();

    private void EnsurePage(int pageIndex)
    {
        while (_pages.Count <= pageIndex)
            _pages.Add(null);
        if (_pages[pageIndex] == null)
        {
            _pages[pageIndex] = new int[PageSize];
            Array.Fill(_pages[pageIndex], -1);
        }
    }

    public void Remove(int key) => this[key] = -1;

    public bool ContainsKey(int id)
    {
        return this[id] != -1;
    }

    public bool TryGetValue(int key, out int value)
    {
        value = this[key];
        return value != -1;
    }
    
    public int this[int key]
    {
        get
        {
            if (key < 0) return -1;
            var pageIndex = key >> PageBits;
            if (pageIndex >= _pages.Count || _pages[pageIndex] == null)
                return -1;
            return _pages[pageIndex][key & PageMask];
        }
        set
        {
            if (key < 0) throw new ArgumentOutOfRangeException(nameof(key));
            var pageIndex = key >> PageBits;
            EnsurePage(pageIndex);
            _pages[pageIndex][key & PageMask] = value;
        }
    }

    public void Clear()
    {
        foreach (var page in _pages)
        {
            if (page != null)
            {
                Array.Fill(page, -1);
            }
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(this);

    public struct Enumerator
    {
        private readonly PagedIdMap map;
        private int pageIndex;
        private int entryIndex;

        public Enumerator(PagedIdMap map)
        {
            this.map = map;
            Current = default;
            pageIndex = 0;
            entryIndex = -1;
        }

        public int Current { get; set; }

        public bool MoveNext()
        {
            while (pageIndex < map._pages.Count)
            {
                var page = map._pages[pageIndex];
                if (page != null)
                {
                    entryIndex++;
                    if (entryIndex < PageSize)
                    {
                        Current = page[entryIndex];
                        if (Current != -1) return true; // found a valid entry
                    }
                }
                // Move to the next page
                entryIndex = -1;
                pageIndex++;
            }
            return false; // no more entries
        }
    }
}