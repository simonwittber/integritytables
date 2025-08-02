using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public class IntMap<T>
{
    
    private const int PageBits = 10;          
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;

    private readonly List<T[]?> _values = new List<T[]?>();
    private readonly IntSet _keys = new IntSet();

    private void EnsurePage(int pageIndex)
    {
        while (_values.Count <= pageIndex)
        {
            _values.Add(null);
        }
        if (_values[pageIndex] == null)
        {
            _values[pageIndex] = new T[PageSize];
        }
    }

    public bool Remove(int v)
    {
        if (!_keys.Remove(v)) return false;
        var u = ZigZagEncode(v);
        var pageIndex = (int) (u >> PageBits);
        var slot = (int)(u & PageMask);
        _values[pageIndex]![slot] = default(T);
        return true;
    }

    public bool ContainsKey(int id) => _keys.Contains(id);

    public bool TryGetValue(int v, out T value)
    {
        if (_keys.Contains(v))
        {
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            var slot = key & PageMask;
            value = _values[pageIndex]![slot];
            return true;
        }
        value = default(T);
        return false;
    }
    
    public T this[int v]
    {
        get
        {
            if (!_keys.Contains(v)) 
                throw new KeyNotFoundException();
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            var slot = key & PageMask;
            return _values[pageIndex]![slot];
        }
        set
        {
            var key = (int) ZigZagEncode(v);
            var pageIndex = key >> PageBits;
            EnsurePage(pageIndex);
            var slot = key & PageMask;
            _values[pageIndex]![slot] = value;
            _keys.Add(v);
        }
    }

    public void Clear()
    {
        foreach (var page in _values)
        {
            if (page != null)
            {
                Array.Fill(page, default);
            }
        }
        _keys.Clear();
    }

    public IntSet.Enumerator GetEnumerator() => _keys.GetEnumerator();
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZigZagEncode(int v) => ((uint) (v << 1)) ^ ((uint) (v >> 31));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ZigZagDecode(uint u) => (int) ((u >> 1) ^ -(u & 1));
}


/// <summary>
/// IntSet is optimized for speed and memory when working with dense sets of small integers.
/// Memory usage is O(max_value / 64). Very large values (over 10M) will cause significant memory allocation.
/// However memory usage is still much lower than a HashSet for the same number of elements.
/// </summary>
public class IntSet
{
    
    private const int PageBits = 6;
    private const int PageSize = 1 << PageBits;
    private const int PageMask = PageSize - 1;
    private const int MaxStackBytes = 128 * 1024; // How much stack space we can use for masks. 
    private const int BytesPerPage = sizeof(ulong);
    private const int MaxPageCount = MaxStackBytes / BytesPerPage;

    private ulong[] _pages;
    private int _pageCount;
    private int _count;
    public int Count => _count;

    public IntSet()
    {
        _pages = new ulong[16];
        _pageCount = 0;
    }

    public IntSet(Span<int> values)
    {
        _pages = new ulong[16];
        _pageCount = 0;
        UnionWith(values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePage(int pageIndex)
    {
        if (pageIndex >= _pages.Length)
        {
            // double until big enough
            var newSize = Math.Max(_pages.Length * 2, pageIndex + 1);
            Array.Resize(ref _pages, newSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Add(int value)
    {
        var key = (int) ZigZagEncode(value);
        // Which page does this key belong to? Each page has 64 bits, so we can store 64 keys per page.
        var p = key >> PageBits;
        // which bit in the page do we set for this key?
        var bit = key & PageMask;
        // create the mask that we |= to set the bit 
        var mask = 1UL << bit;
        // make sure the page exists in our _pages array
        EnsurePage(p);
        var page = _pages[p];
        _pages[p] |= mask;
        // if we have created a new page, increment pageCount
        _pageCount = p >= _pageCount ? p + 1 : _pageCount;
        // return true if we set the bit, false if it was already set
        if ((page & mask) != 0)
            return false;
        _count++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(int value)
    {
        var key = (int) ZigZagEncode(value);
        // get page
        var p = key >> PageBits;
        if (p >= _pageCount) return false;
        // get bit position
        var bit = key & PageMask;
        var mask = (1UL << bit);
        return (_pages[p] & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int value)
    {
        var key = (int) ZigZagEncode(value);
        // get page
        var p = key >> PageBits;
        if (p >= _pageCount) return false;
        // get bit position
        var bit = key & PageMask;
        var mask = 1UL << bit;
        var page = _pages[p];
        if ((page & mask) == 0)
            return false; // wasn’t present

        var newPage = page & ~mask;
        _pages[p] = newPage;

        _count--;
        return true;
    }

    public void IntersectWith(Span<int> span)
    {
        var pageCount = _pageCount;

        var masks = pageCount < MaxPageCount ? stackalloc ulong[pageCount] : new ulong[pageCount];
        if (pageCount <= MaxPageCount)
            masks.Clear();

        foreach (var value in span)
        {
            var v = (int) ZigZagEncode(value);
            if (v < 0) continue;
            // get page
            var p = v >> PageBits;
            if (p >= pageCount) continue;
            // get bit position
            var bit = v & PageMask;
            var mask = 1UL << bit;
            masks[p] |= mask;
        }

        for (var i = 0; i < _pageCount; i++)
            _pages[i] &= masks[i];

        var newCount = 0;

        for (var i = 0; i < _pageCount; i++)
        {
            // intersect the page
            var bits = _pages[i] & masks[i];
            _pages[i] = bits;

            if (bits != 0)
            {
                newCount += PopCount(bits);
            }
        }

        _count = newCount;
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
        var maxPage = _pageCount;
        foreach (var value in span)
        {
            var v = (int) ZigZagEncode(value);
            if (v < 0) continue;
            var p = v >> PageBits;
            if (p + 1 > maxPage) maxPage = p + 1;
        }

        if (maxPage > _pages.Length)
            Array.Resize(ref _pages, maxPage);
        _pageCount = maxPage;

        var masks = maxPage <= MaxPageCount ? stackalloc ulong[maxPage] : new ulong[maxPage];
        if (maxPage > MaxPageCount)
            masks.Clear();

        foreach (var value in span)
        {
            var v = (int) ZigZagEncode(value);
            var p = v >> PageBits;
            var bit = v & PageMask;
            masks[p] |= 1UL << bit;
        }

        var newCount = 0;

        for (var p = 0; p < maxPage; p++)
        {
            var oldBits = _pages[p];
            var flip = masks[p];
            if (flip != 0UL)
                _pages[p] = oldBits ^ flip;

            var bits = _pages[p];
            if (bits != 0UL)
            {
                newCount += PopCount(bits);
            }
        }
        _count = newCount;
    }

    public Enumerator GetEnumerator() => new Enumerator(_pages, _pageCount);

    public struct Enumerator
    {
        private readonly ulong[] _pages;
        private readonly int _pageCount;
        private int _currentPageIndex;
        private ulong _currentBits;
        private int _currentPageBase;

        public int Current { get; private set; }

        internal Enumerator(ulong[] pages, int pageCount)
        {
            _pages = pages;
            _pageCount = pageCount;
            _currentPageIndex = -1;
            _currentBits = 0;
            _currentPageBase = 0;
            Current = 0;
        }

        public bool MoveNext()
        {
            // Extract remaining bits from current page
            if (_currentBits != 0)
            {
                var tz = TrailingZeroCount(_currentBits);
                _currentBits &= _currentBits - 1; // Clear lowest bit
                Current = ZigZagDecode((uint) (_currentPageBase | tz));
                return true;
            }

            // Move to next page
            var pageCount = _pageCount;
            while (++_currentPageIndex < pageCount)
            {
                _currentBits = _pages[_currentPageIndex];
                if (_currentBits == 0) continue;
                _currentPageBase = _currentPageIndex << PageBits;
                var tz = TrailingZeroCount(_currentBits);
                _currentBits &= _currentBits - 1; // Clear lowest bit
                Current = ZigZagDecode((uint) (_currentPageBase | tz));
                return true;
            }

            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZigZagEncode(int v) => ((uint) (v << 1)) ^ ((uint) (v >> 31));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ZigZagDecode(uint u) => (int) ((u >> 1) ^ -(u & 1));

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


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TrailingZeroCount(ulong b)
    {
        return MultiplyDeBruijnBitPosition[((ulong) ((long) b & -(long) b) * DeBruijnSequence) >> 58];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int PopCount(ulong x)
    {
        x = x - ((x >> 1) & 0x5555_5555_5555_5555UL);
        x = (x & 0x3333_3333_3333_3333UL) + ((x >> 2) & 0x3333_3333_3333_3333UL);
        x = (x + (x >> 4)) & 0x0F0F_0F0F_0F0F_0F0FUL;
        return (int) ((x * 0x0101_0101_0101_0101UL) >> 56);
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
        _count = 0;
    }
}