using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public class IdSet
{
    private const int PageBits = 6;
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;

    private ulong[] _pages;
    private int _pageCount;
    private List<int> _activePages = new();
    private int _count;
    public int Count => _count;
    
    const int MaxStackBytes = 64 * 1024; // How much stack space we can use for masks. 
    const int BytesPerPage = sizeof(ulong);
    const int MaxPageCount = MaxStackBytes / BytesPerPage;

    public IdSet()
    {
        _pages = new ulong[16];
        _pageCount = 0;
    }

    public IdSet(Span<int> values)
    {
        _pages = new ulong[16];
        _pageCount = 0;
        this.UnionWith(values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePage(int pageIndex)
    {
        if (pageIndex >= _pages.Length)
        {
            // double until big enough
            int newSize = Math.Max(_pages.Length * 2, pageIndex + 1);
            Array.Resize(ref _pages, newSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(int key)
    {
        if(key < 0) throw new ArgumentOutOfRangeException(nameof(key));
        // Which page does this key belong to? Each page has 64 bits, so we can store 64 keys per page.
        int p = key >> PageBits;
        // which bit in the page do we set for this key?
        int bit = key & PageMask;
        // create the mask that we |= to set the bit 
        ulong mask = 1UL << bit;
        // make sure the page exists in our _pages array
        EnsurePage(p);
        ulong page = _pages[p];
        _pages[p] |= mask;
        // if we have created a new page, increment pageCount
        _pageCount = p >= _pageCount ? p + 1 : _pageCount;
        // if this page was empty before, we need to add it to _livePages
        if (page == 0UL)
            _activePages.Add(p);
        // return true if we set the bit, false if it was already set
        if((page & mask) != 0)
            return false;
        _count++;
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int key)
    {
        if (key < 0) return false;
        // get page
        int p = key >> PageBits;
        if (p >= _pageCount) return false;
        // get bit position
        int bit = key & PageMask;
        var mask = (1UL << bit);
        return (_pages[p] & mask) != 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int key)
    {
        if (key < 0) return false;
        // get page
        int p = key >> PageBits;
        if (p >= _pageCount) return false;
        // get bit position
        int bit = key & PageMask;
        ulong mask = 1UL << bit;
        ulong page = _pages[p];
        if ((page & mask) == 0)
            return false; // wasn’t present

        ulong newPage = page & ~mask;
        _pages[p] = newPage;

        // if we emptied the page (no more bits are set), remove it from _activePages
        if (newPage == 0UL)
        {
            int idx = _activePages.IndexOf(p);
            int last = _activePages.Count - 1;
            _activePages[idx] = _activePages[last];
            _activePages.RemoveAt(last);
        }
        _count--;
        return true;
    }

    public void IntersectWith(Span<int> span)
    {
        int pageCount = _pageCount;

        

        Span<ulong> masks = pageCount < MaxPageCount ? stackalloc ulong[pageCount] : new ulong[pageCount];
        if (pageCount <= MaxPageCount)
            masks.Clear();

        foreach (var v in span)
        {
            if (v < 0) continue;
            // get page
            int p = v >> PageBits;
            if (p >= pageCount) continue;
            // get bit position
            int bit = v & PageMask;
            var mask = 1UL << bit;
            masks[p] |= mask;
        }

        for (int i = 0; i < _pageCount; i++)
            _pages[i] &= masks[i];
        
        
        int newCount = 0;

        for (int i = _activePages.Count-1; i >= 0; i--)
        {
            int pageIndex = _activePages[i];
            // intersect the page
            ulong bits = _pages[pageIndex] & masks[pageIndex];
            _pages[pageIndex] = bits;

            if (bits != 0)
            {
                newCount += PopCount(bits);
            }
            else
            {
                var last = _activePages.Count - 1;
                if (i < last)
                {
                    _activePages[i] = _activePages[last];
                }
                _activePages.RemoveAt(last);
                _pageCount--;
            }
        }
        _count       = newCount;
    }

    public void UnionWith(Span<int> span)
    {
        foreach (var v in span)
        {
            Add(v);
        }
    }
    
    public void ExceptWith(Span<int> span)
    {
        foreach (var v in span)
        {
            Remove(v);
        }
    }

    public void SymmetricExceptWith(Span<int> span)
    {
        // 1) Figure out how many pages we need
        int maxPage = _pageCount;
        foreach (var v in span)
        {
            if (v < 0) continue;
            int p = v >> PageBits;
            if (p + 1 > maxPage) maxPage = p + 1;
        }

        // 2) Ensure our pages array is large enough
        if (maxPage > _pages.Length)
            Array.Resize(ref _pages, maxPage);
        _pageCount = maxPage;

        // 3) Build the “unique‑element” mask
        Span<ulong> masks = maxPage <= MaxPageCount
            ? stackalloc ulong[maxPage]
            : new ulong[maxPage];
        masks.Clear();

        foreach (var v in span)
        {
            if (v < 0) continue;
            int p   = v >> PageBits;
            int bit = v & PageMask;
            masks[p] |= 1UL << bit;         // OR’ing ignores duplicates
        }

        // 4) XOR the masks into pages, rebuild active pages & count
        var newActive = new List<int>( _activePages.Count );
        int newCount  = 0;

        for (int p = 0; p < maxPage; p++)
        {
            ulong oldBits = _pages[p];
            ulong flip    = masks[p];
            if (flip != 0UL)
                _pages[p] = oldBits ^ flip; // flip each unique bit

            ulong bits = _pages[p];
            if (bits != 0UL)
            {
                newActive.Add(p);
                newCount += PopCount(bits);  // fast 64‑bit popcount
            }
        }

        // 5) Install the refreshed state
        _activePages = newActive;
        _count       = newCount;
    }

    public Enumerator GetEnumerator() => new Enumerator(_pages, _activePages);

    public struct Enumerator
    {
        private readonly ulong[] _pages;
        private readonly List<int> _activePages;
        private readonly int _pageCount;
        private int _currentPageIndex;
        private ulong _currentBits;
        private int _currentPageBase;

        public int Current { get; private set; }

        internal Enumerator(ulong[] pages, List<int> activePages)
        {
            _pages = pages;
            _activePages = activePages;
            _pageCount = activePages.Count;
            _currentPageIndex = -1;
            _currentBits = 0;
            _currentPageBase = 0;
            Current = 0;
        }

        public bool MoveNext()
        {
            // Extract remaining bits from current page
            while (_currentBits != 0)
            {
                int tz = TrailingZeroCount(_currentBits);
                _currentBits &= _currentBits - 1; // Clear lowest bit
                Current = _currentPageBase | tz;
                return true;
            }

            // Move to next page
            while (++_currentPageIndex < _pageCount)
            {
                int pageIndex = _activePages[_currentPageIndex];
                _currentBits = _pages[pageIndex];

                if (_currentBits == 0) continue;
                _currentPageBase = pageIndex << PageBits;
                int tz = TrailingZeroCount(_currentBits);
                _currentBits &= _currentBits - 1; // Clear lowest bit
                Current = _currentPageBase | tz;
                return true;
            }

            return false;
        }
    }

    private const ulong DeBruijnSequence = 0x37E84A99DAE458F;

    private static readonly int[] MultiplyDeBruijnBitPosition =
    {
        0, 1, 17, 2, 18, 50, 3, 57,
        47, 19, 22, 51, 29, 4, 33, 58,
        15, 48, 20, 27, 25, 23, 52, 41,
        54, 30, 38, 5, 43, 34, 59, 8,
        63, 16, 49, 56, 46, 21, 28, 32,
        14, 26, 24, 40, 53, 37, 42, 7,
        62, 55, 45, 31, 13, 39, 36, 6,
        61, 44, 12, 35, 60, 11, 10, 9,
    };

    /// <summary>
    /// Search the mask data from least significant bit (LSB) to the most significant bit (MSB) for a set bit (1)
    /// using De Bruijn sequence approach. Warning: Will return zero for b = 0.
    /// </summary>
    /// <param name="b">Target number.</param>
    /// <returns>Zero-based position of LSB (from right to left).</returns>
    private static int TrailingZeroCount(ulong b)
    {
        return MultiplyDeBruijnBitPosition[((ulong)((long)b & -(long)b) * DeBruijnSequence) >> 58];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PopCount(ulong x)
    {
        // “Hacker’s Delight”-style 64‑bit popcount
        x = x - ((x >> 1) & 0x5555_5555_5555_5555UL);
        x = (x & 0x3333_3333_3333_3333UL) + ((x >> 2) & 0x3333_3333_3333_3333UL);
        x = (x + (x >> 4)) & 0x0F0F_0F0F_0F0F_0F0FUL;
        return (int)((x * 0x0101_0101_0101_0101UL) >> 56);
    }
    
    public int[] ToArray()
    {
        var list = new List<int>();
        foreach (var i in this)
            list.Add(i);
        return list.ToArray();
    }
    
    public List<int> ToList()
    {
        var list = new List<int>();
        foreach (var i in this)
            list.Add(i);
        return list;
    }


    public void Clear()
    {
        for (var i = 0; i < _pageCount; i++)
            _pages[i] = 0ul;
        _pageCount = 0;
        _activePages.Clear();
        _count = 0;
    }
}