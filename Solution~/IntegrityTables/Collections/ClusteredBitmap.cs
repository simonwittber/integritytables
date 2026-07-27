using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;


/// <summary>
/// A set or bitmap of index values (positive integers) centered around the initial value passed into Add.
/// Use case: when values are very large, but are near each other, for example GetInstanceId().
/// In this use case, IntSet class would use a very large amount of memory, however the ClusteredBitmap class will not.
/// This feature limits the and, not, or and xor operations to only operating on Spans, as ClusteredBitmaps with different center values are not compatible with each other.
/// A ClusteredBitmap has the ability to work with negative and positive integers.
/// </summary>
public class ClusteredBitmap 
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

    private int _centerValue;

    public int Count => _count;
    public int PageCount => _pageCount;

    public ClusteredBitmap()
    {
        _pages = new ulong[16];
        _pageCount = 0;
    }

    public ClusteredBitmap(Span<int> values)
    {
        _pages = new ulong[16];
        _pageCount = 0;
        Or(values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsurePage(int pageIndex)
    {
        if (pageIndex >= _pages.Length)
        {
            var newSize = Math.Max(_pages.Length * 2, pageIndex + 1);
            Array.Resize(ref _pages, newSize);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Set(int value)
    {
        if(_count == 0) 
            _centerValue = value;
        // convert the value to a key, which is a uint integer
        GetPageAndBit(value, out var pageIndex, out var mask);
        // make sure the page exists in our _pages array
        EnsurePage(pageIndex);
        if (IsSet(pageIndex, mask)) return false;
        SetBit(pageIndex, mask);
        // if we have created a new page, increment pageCount
        _pageCount = pageIndex >= _pageCount ? pageIndex + 1 : _pageCount;
        _count++;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(int p, ulong mask) => _pages[p] |= mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void UnSetBit(int p, ulong mask) => _pages[p] &= ~mask;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSet(int p, ulong mask) => (_pages[p] & mask) != 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetPageAndBit(int value, out int pageIndex, out ulong mask)
    {
        var key = ZigZagEncode(value - _centerValue);
        pageIndex = (int) (key >> PageBits);
        var bitIndex = (int) (key & PageMask);
        mask = 1ul << bitIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSet(int value)
    {
        GetPageAndBit(value, out var pageIndex, out var mask);
        return pageIndex < _pageCount && IsSet(pageIndex, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool UnSet(int value)
    {
        GetPageAndBit(value, out var pageIndex, out var mask);
        if (pageIndex >= _pageCount) return false;
        if (IsSet(pageIndex, mask))
        {
            UnSetBit(pageIndex, mask);
            _count--;
            return true;
        }

        return false;
    }

    public void And(Span<int> span)
    {
        var pageCount = _pageCount;

        var masks = pageCount > MaxPageCount ? new ulong[pageCount] : stackalloc ulong[pageCount];
        if (pageCount > MaxPageCount)
            masks.Clear();

        // Collapse duplicate values in the span into masks for each page.
        foreach (var value in span)
        {
            GetPageAndBit(value, out var pageIndex, out var mask);
            if (pageIndex >= pageCount) continue;
            masks[pageIndex] |= mask;
        }

        var pages = _pages;
        for (var i = 0; i < _pageCount; i++)
            pages[i] &= masks[i];

        Recount();
    }
    
    public void Or(Span<int> span)
    {
        var maxPageIndex = _pageCount - 1;
        foreach (var value in span)
        {
            GetPageAndBit(value, out var pageIndex, out _);
            maxPageIndex = Math.Max(maxPageIndex, pageIndex);
        }

        if (maxPageIndex >= 0)
        {
            EnsurePage(maxPageIndex);
            _pageCount = Math.Max(_pageCount, maxPageIndex + 1);
        }
        var pageCount = _pageCount;

        var masks = pageCount > MaxPageCount ? new ulong[pageCount] : stackalloc ulong[pageCount];
        if (pageCount > MaxPageCount)
            masks.Clear();

        // Collapse duplicate values in the span into masks for each page.
        foreach (var value in span)
        {
            GetPageAndBit(value, out var pageIndex, out var mask);
            if (pageIndex >= pageCount) continue;
            masks[pageIndex] |= mask;
        }

        var pages = _pages;
        for (var i = 0; i < _pageCount; i++)
            pages[i] |= masks[i];

        Recount();
    }

    public void Not(Span<int> span)
    {
        foreach (var v in span)
        {
            UnSet(v);
        }
    }

    public void Xor(Span<int> span)
    {
        var pageCount = _pageCount;
        var masks = pageCount > MaxPageCount ? new ulong[pageCount] : stackalloc ulong[pageCount];
        if (pageCount > MaxPageCount)
            masks.Clear();

        // Collapse duplicate values in the span into masks for each page.
        foreach (var value in span)
        {
            GetPageAndBit(value, out var pageIndex, out var mask);
            if (pageIndex >= pageCount)
                Set(value);
            else
                masks[pageIndex] |= mask;
        }

        var pages = _pages;
        var minCount = Math.Min(_pageCount, masks.Length);
        for (var i = 0; i < minCount; i++)
        {
            pages[i] ^= masks[i]; // XOR the masks with the current pages
        }

        Recount();
    }

    public void Clear()
    {
        for (var i = 0; i < _pageCount; i++)
            _pages[i] = 0ul;
        _pageCount = 0;
        _count = 0;
        _centerValue = 0;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Recount()
    {
        var pages = _pages;
        var count = 0;
        for (int i = 0, end = _pageCount; i < end; i++)
            if (pages[i] != 0UL)
                count += PopCount(pages[i]);
        _count = count;
    }

    public Enumerator GetEnumerator() => new Enumerator(_pages, _pageCount, _centerValue);

    public struct Enumerator
    {
        private readonly ulong[] _pages;
        private readonly int _pageCount;
        private int _currentPageIndex;
        private ulong _currentPage;
        private int _currentPageBase;
        private int _centerValue;

        public Enumerator GetEnumerator() => this;

        public void Reset()
        {
            _currentPageIndex = -1;
            _currentPage = 0;
            _currentPageBase = 0;
            Current = 0;
        }

        public int Current { get; private set; }

        internal Enumerator(ulong[] pages, int pageCount, int centerValue)
        {
            _pages = pages;
            _pageCount = pageCount;
            _currentPageIndex = -1;
            _currentPage = 0;
            _currentPageBase = 0;
            _centerValue = centerValue;
            Current = 0;
        }

        public bool MoveNext()
        {
            // Extract remaining bits from current page
            if (_currentPage != 0)
            {
                var bitPosition = TrailingZeroCount(_currentPage);
                _currentPage &= _currentPage - 1; // Clear lowest bit
                Current = ZigZagDecode((uint)(_currentPageBase | bitPosition)) + _centerValue;
                return true;
            }

            // Move to next page
            var pageCount = _pageCount;
            while (++_currentPageIndex < pageCount)
            {
                _currentPage = _pages[_currentPageIndex];
                if (_currentPage == 0) continue;
                // currentPageBase is the base value for the current page, a multiple of 64
                _currentPageBase = _currentPageIndex << PageBits;
                // any bit position set in the current page needs be added to currentPageBase
                // to get the the original value
                var bitPosition = TrailingZeroCount(_currentPage);
                _currentPage &= _currentPage - 1; // Clear lowest bit
                Current = ZigZagDecode((uint)(_currentPageBase | bitPosition)) + _centerValue;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
    }

    private const ulong DeBruijnSequence = 0x37E84A99DAE458F;

    private static readonly int[] MultiplyDeBruijnBitPosition = {0, 1, 17, 2, 18, 50, 3, 57, 47, 19, 22, 51, 29, 4, 33, 58, 15, 48, 20, 27, 25, 23, 52, 41, 54, 30, 38, 5, 43, 34, 59, 8, 63, 16, 49, 56, 46, 21, 28, 32, 14, 26, 24, 40, 53, 37, 42, 7, 62, 55, 45, 31, 13, 39, 36, 6, 61, 44, 12, 35, 60, 11, 10, 9,};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TrailingZeroCount(ulong b)
    {
        return MultiplyDeBruijnBitPosition[((ulong) ((long) b & -(long) b) * DeBruijnSequence) >> 58];
    }
    
    /// <summary>
    /// Converts negative and positive integers to a non-negative integer using ZigZag encoding.
    /// -1 => 1, -2 => 3, 0 => 0, 1 => 2, 2 => 4, etc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ZigZagEncode(int v)
    {
        return ((uint) (v << 1)) ^ ((uint) (v >> 31));
    }

    /// <summary>
    /// Converts a non-negative integer back to a signed integer using ZigZag decoding.
    /// 1 => -1, 3 => -2, 0 => 0, 2 => 1, 4 => 2, etc.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ZigZagDecode(uint u)
    {
        return (int) ((u >> 1) ^ -(u & 1));
    }

    /// <summary>
    /// Returns the number of bits set to 1 in the given ulong value.
    /// </summary>
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
        var array = new int[_count];
        var index = 0;
        foreach (var i in this)
        {
            array[index++] = i;
        }
        return array;
    }
    
    public List<int> ToList()
    {
        var array = new List<int>(_count);
        foreach (var i in this)
        {
            array.Add(i);
        }
        return array;
    }
}