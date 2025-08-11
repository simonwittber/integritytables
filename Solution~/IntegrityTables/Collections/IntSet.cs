using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public interface IReadOnlyIntSet
{
    int Count { get; }
    bool Contains(int value);
    int[] ToArray();
    List<int> ToList();
}

/// <summary>
/// A memory-efficient set for storing integers, optimized for dense ranges of integer keys.
/// Note, a sparse, large range is not stored efficiently.
/// Use this class for fast membership checks and set operations on integers.
/// </summary>
public class IntSet : IEnumerable<int>, IReadOnlyIntSet
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
    private int _initialKey;
    private bool _isInitialized;

    public int Count => _count;
    
    public int PageCount => _pageCount;

    public IntSet()
    {
        _pages = new ulong[16];
        _pageCount = 0;
        _initialKey = 0;
        _isInitialized = false;
    }

    public IntSet(Span<int> values)
    {
        _pages = new ulong[16];
        _pageCount = 0;
        _initialKey = 0;
        _isInitialized = false;
        UnionWith(values);
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
    public bool Add(int value)
    {
        _initialKey = _isInitialized ? _initialKey : value;
        _isInitialized = true;
        value -= _initialKey; // Used to normalize future values to start from closer to zero
        var key = ZigZagEncode(value);
        // Which page does this key belong to? Each page has 64 bits, so we can store 64 keys per page.
        var p = (int) (key >> PageBits);
        // which bit in the page do we set for this key?
        var bit = (int) (key & PageMask);
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
        value -= _initialKey;
        var key = ZigZagEncode(value);
        // get page
        var p = (int) (key >> PageBits);
        if (p >= _pageCount) return false;
        // get bit position
        var bit = (int) (key & PageMask);
        var mask = (1UL << bit);
        return p < _pages.Length && (_pages[p] & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Remove(int value)
    {
        value -= _initialKey;
        var key = ZigZagEncode(value);
        // get page
        var p = (int) (key >> PageBits);
        if (p >= _pageCount) return false;
        // get bit position
        var bit = (int) (key & PageMask);
        var mask = 1UL << bit;
        var page = _pages[p];
        if ((page & mask) == 0)
            return false;

        var newPage = page & ~mask;
        _pages[p] = newPage;

        _count--;
        return true;
    }

    public void IntersectWith(Span<int> span)
    {
        var pageCount = _pageCount;

        var masks = pageCount > MaxPageCount ? new ulong[pageCount] : stackalloc ulong[pageCount];
        if (pageCount > MaxPageCount)
            masks.Clear();

        foreach (var value in span)
        {
            var v = value - _initialKey;
            var key = ZigZagEncode(v);
            // get page
            var p = (int) (key >> PageBits);
            if (p >= pageCount) continue;
            // get bit position
            var bit = (int) (key & PageMask);
            var mask = 1UL << bit;
            masks[p] |= mask;
        }

        var pages = _pages;
        for (var i = 0; i < _pageCount; i++)
            pages[i] &= masks[i];

        var newCount = 0;

        for (var i = 0; i < _pageCount; i++)
        {
            // intersect the page
            var bits = pages[i] & masks[i];
            pages[i] = bits;

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
            var key = ZigZagEncode(value - _initialKey);
            var p = (int) (key >> PageBits);
            if (p + 1 > maxPage) maxPage = p + 1;
        }

        var pages = _pages;
        if (maxPage > pages.Length)
            Array.Resize(ref pages, maxPage);
        _pageCount = maxPage;

        var masks = maxPage > MaxPageCount ? new ulong[maxPage] : stackalloc ulong[maxPage];
        if (maxPage > MaxPageCount)
            masks.Clear();

        foreach (var value in span)
        {
            var key = ZigZagEncode(value - _initialKey);
            var p = (int) (key >> PageBits);
            var bit = (int) (key & PageMask);
            masks[p] |= 1UL << bit;
        }

        var newCount = 0;

        for (var p = 0; p < maxPage; p++)
        {
            var oldBits = pages[p];
            var flip = masks[p];
            if (flip != 0UL)
                pages[p] = oldBits ^ flip;

            var bits = pages[p];
            if (bits != 0UL)
            {
                newCount += PopCount(bits);
            }
        }

        _count = newCount;
    }

    public Enumerator GetEnumerator() => new Enumerator(_pages, _pageCount, _initialKey);
    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public Enumerator GetEnumeratorForPageRange(int startPage, int endPage)
    {
        return new Enumerator(_pages, startPage, endPage, _initialKey);
    }

    public struct Enumerator : IEnumerator<int>
    {
        private readonly ulong[] _pages;
        private readonly int _pageCount;
        private int _currentPageIndex;
        private ulong _currentBits;
        private int _currentPageBase;
        private readonly int _initialKey;

        public Enumerator GetEnumerator() => this;

        public void Reset()
        {
            _currentPageIndex = -1;
            _currentBits = 0;
            _currentPageBase = 0;
            Current = 0;
        }

        object? IEnumerator.Current { get; }
        public int Current { get; private set; }

        internal Enumerator(ulong[] pages, int pageCount, int initialKey)
        {
            _pages = pages;
            _pageCount = pageCount;
            _currentPageIndex = -1;
            _currentBits = 0;
            _currentPageBase = 0;
            _initialKey = initialKey;
            Current = 0;
        }
        
        internal Enumerator(ulong[] pages, int startPage, int endPage, int initialKey)
        {
            _pages = pages;
            _pageCount = endPage; // Exclusive end
            _currentPageIndex = startPage - 1; // Start before first page
            _currentBits = 0;
            _currentPageBase = 0;
            _initialKey = initialKey;
            Current = 0;
        }

        public bool MoveNext()
        {
            // Extract remaining bits from current page
            if (_currentBits != 0)
            {
                var tz = TrailingZeroCount(_currentBits);
                _currentBits &= _currentBits - 1; // Clear lowest bit
                Current = ZigZagDecode((uint) (_currentPageBase | tz)) + _initialKey;
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
                Current = ZigZagDecode((uint) (_currentPageBase | tz)) + _initialKey;
                return true;
            }

            return false;
        }

        public void Dispose()
        {
        }
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

    private const ulong DeBruijnSequence = 0x37E84A99DAE458F;

    private static readonly int[] MultiplyDeBruijnBitPosition = {0, 1, 17, 2, 18, 50, 3, 57, 47, 19, 22, 51, 29, 4, 33, 58, 15, 48, 20, 27, 25, 23, 52, 41, 54, 30, 38, 5, 43, 34, 59, 8, 63, 16, 49, 56, 46, 21, 28, 32, 14, 26, 24, 40, 53, 37, 42, 7, 62, 55, 45, 31, 13, 39, 36, 6, 61, 44, 12, 35, 60, 11, 10, 9,};

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int TrailingZeroCount(ulong b)
    {
        return MultiplyDeBruijnBitPosition[((ulong) ((long) b & -(long) b) * DeBruijnSequence) >> 58];
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
        return new List<int>(ToArray());
    }

    public void Clear()
    {
        for (var i = 0; i < _pageCount; i++)
            _pages[i] = 0ul;
        _pageCount = 0;
        _count = 0;
        _initialKey = 0;
        _isInitialized = false;
    }

    public void UnionWith(IReadOnlyIntSet other)
    {
        UnionWith((IntSet) other);
    }

    public void UnionWith(IntSet other)
    {
        if (other._count == 0) return;
        if (_count == 0)
        {
            // Fast path: copy everything from other
            _initialKey = other._initialKey;
            _isInitialized = other._isInitialized;
            _count = other._count;
            _pageCount = other._pageCount;

            if (other._pageCount > _pages.Length)
                Array.Resize(ref _pages, other._pageCount);

            Array.Copy(other._pages, _pages, other._pageCount);
            return;
        }

        // Handle different initial keys by normalizing to the smaller one
        var keyDiff = other._initialKey - _initialKey;
        if (keyDiff != 0)
        {
            if (keyDiff < 0)
            {
                // Other set has smaller initial key, shift this set
                ShiftPages(-keyDiff);
                _initialKey = other._initialKey;
            }
            else
            {
                // This set has smaller initial key, need to shift other's pages during union
                UnionWithOffset(other, keyDiff);
                return;
            }
        }

        // Same initial keys - direct page union
        var maxPageCount = Math.Max(_pageCount, other._pageCount);
        if (maxPageCount > _pages.Length)
            Array.Resize(ref _pages, maxPageCount);

        var newCount = 0;
        for (var i = 0; i < maxPageCount; i++)
        {
            var oldBits = i < _pageCount ? _pages[i] : 0UL;
            var otherBits = i < other._pageCount ? other._pages[i] : 0UL;
            var newBits = oldBits | otherBits;
            _pages[i] = newBits;

            if (newBits != 0UL)
                newCount += PopCount(newBits);
        }

        _pageCount = maxPageCount;
        _count = newCount;
    }

    private void ShiftPages(int keyOffset)
    {
        var pageOffset = keyOffset >> PageBits;
        var bitOffset = keyOffset & PageMask;

        if (bitOffset == 0)
        {
            // Simple page-aligned shift
            var newPageCount = _pageCount + pageOffset;
            if (newPageCount > _pages.Length)
                Array.Resize(ref _pages, newPageCount);

            Array.Copy(_pages, 0, _pages, pageOffset, _pageCount);
            Array.Clear(_pages, 0, pageOffset);
            _pageCount = newPageCount;
        }
        else
        {
            // Bit-level shift across page boundaries
            var newPageCount = _pageCount + pageOffset + 1;
            if (newPageCount > _pages.Length)
                Array.Resize(ref _pages, newPageCount);

            var rightShift = PageSize - bitOffset;
            ulong carry = 0;

            for (var i = _pageCount - 1; i >= 0; i--)
            {
                var currentBits = _pages[i];
                _pages[i + pageOffset + 1] = (currentBits >> rightShift) | carry;
                carry = currentBits << bitOffset;
            }

            _pages[pageOffset] = carry;

            Array.Clear(_pages, 0, pageOffset);
            _pageCount = newPageCount;
        }
    }

    private void UnionWithOffset(IntSet other, int keyOffset)
    {
        var pageOffset = keyOffset >> PageBits;
        var bitOffset = keyOffset & PageMask;

        var maxPageCount = Math.Max(_pageCount, other._pageCount + pageOffset + 1);
        if (maxPageCount > _pages.Length)
            Array.Resize(ref _pages, maxPageCount);

        if (bitOffset == 0)
        {
            // Page-aligned union
            for (var i = 0; i < other._pageCount; i++)
            {
                _pages[i + pageOffset] |= other._pages[i];
            }
        }
        else
        {
            // Bit-shifted union
            var rightShift = PageSize - bitOffset;
            ulong carry = 0;

            for (var i = 0; i < other._pageCount; i++)
            {
                var otherBits = other._pages[i];
                _pages[i + pageOffset] |= (otherBits << bitOffset) | carry;
                carry = otherBits >> rightShift;
            }

            if (carry != 0)
                _pages[other._pageCount + pageOffset] |= carry;
        }

        // Recount total bits
        var newCount = 0;
        _pageCount = maxPageCount;
        for (var i = 0; i < _pageCount; i++)
        {
            if (_pages[i] != 0UL)
                newCount += PopCount(_pages[i]);
        }

        _count = newCount;
    }

    private void SymmetricExceptWithSameKey(IntSet other)
    {
        var maxPageCount = Math.Max(_pageCount, other._pageCount);
        if (maxPageCount > _pages.Length)
            Array.Resize(ref _pages, maxPageCount);

        var newCount = 0;
        for (var i = 0; i < maxPageCount; i++)
        {
            var thisBits = i < _pageCount ? _pages[i] : 0UL;
            var otherBits = i < other._pageCount ? other._pages[i] : 0UL;
            var xorResult = thisBits ^ otherBits;
            _pages[i] = xorResult;

            if (xorResult != 0UL)
                newCount += PopCount(xorResult);
        }

        _pageCount = maxPageCount;
        _count = newCount;
    }

    private void SymmetricExceptWithOffset(IntSet other, int keyOffset)
    {
        var pageOffset = keyOffset >> PageBits;
        var bitOffset = keyOffset & PageMask;

        var maxPageCount = Math.Max(_pageCount, other._pageCount + pageOffset + 1);
        if (maxPageCount > _pages.Length)
            Array.Resize(ref _pages, maxPageCount);

        if (bitOffset == 0)
        {
            // Page-aligned symmetric difference
            for (var i = 0; i < other._pageCount; i++)
            {
                _pages[i + pageOffset] ^= other._pages[i];
            }
        }
        else
        {
            // Bit-shifted symmetric difference
            var rightShift = PageSize - bitOffset;
            ulong carry = 0;

            for (var i = 0; i < other._pageCount; i++)
            {
                var otherBits = other._pages[i];
                _pages[i + pageOffset] ^= (otherBits << bitOffset) | carry;
                carry = otherBits >> rightShift;
            }

            if (carry != 0)
                _pages[other._pageCount + pageOffset] ^= carry;
        }

        // Recount total bits
        var newCount = 0;
        _pageCount = maxPageCount;
        for (var i = 0; i < _pageCount; i++)
        {
            if (_pages[i] != 0UL)
                newCount += PopCount(_pages[i]);
        }

        _count = newCount;
    }

    private void ExceptWithSameKey(IntSet other)
    {
        var newCount = 0;
        var minPageCount = Math.Min(_pageCount, other._pageCount);

        // Subtract overlapping pages
        for (var i = 0; i < minPageCount; i++)
        {
            var subtracted = _pages[i] & ~other._pages[i];
            _pages[i] = subtracted;
            if (subtracted != 0UL)
                newCount += PopCount(subtracted);
        }

        // Count remaining pages beyond other's range
        for (var i = minPageCount; i < _pageCount; i++)
        {
            if (_pages[i] != 0UL)
                newCount += PopCount(_pages[i]);
        }

        _count = newCount;
    }

    private void ExceptWithOffset(IntSet other, int keyOffset)
    {
        var pageOffset = keyOffset >> PageBits;
        var bitOffset = keyOffset & PageMask;
        var newCount = 0;

        if (bitOffset == 0)
        {
            // Page-aligned subtraction
            var startPage = Math.Max(0, pageOffset);
            var endPage = Math.Min(_pageCount, other._pageCount + pageOffset);

            // Count pages before overlap (unchanged)
            for (var i = 0; i < startPage; i++)
            {
                if (_pages[i] != 0UL)
                    newCount += PopCount(_pages[i]);
            }

            // Subtract overlapping pages
            for (var i = startPage; i < endPage; i++)
            {
                var otherIndex = i - pageOffset;
                var subtracted = _pages[i] & ~other._pages[otherIndex];
                _pages[i] = subtracted;
                if (subtracted != 0UL)
                    newCount += PopCount(subtracted);
            }

            // Count pages after overlap (unchanged)
            for (var i = endPage; i < _pageCount; i++)
            {
                if (_pages[i] != 0UL)
                    newCount += PopCount(_pages[i]);
            }
        }
        else
        {
            // Bit-shifted subtraction
            var rightShift = PageSize - bitOffset;

            for (var i = 0; i < _pageCount; i++)
            {
                var otherBits = 0UL;
                var otherIndex = i - pageOffset;

                if (otherIndex >= 0 && otherIndex < other._pageCount)
                    otherBits |= other._pages[otherIndex] << bitOffset;

                if (otherIndex - 1 >= 0 && otherIndex - 1 < other._pageCount)
                    otherBits |= other._pages[otherIndex - 1] >> rightShift;

                var subtracted = _pages[i] & ~otherBits;
                _pages[i] = subtracted;
                if (subtracted != 0UL)
                    newCount += PopCount(subtracted);
            }
        }

        _count = newCount;
    }

    private void IntersectWithSameKey(IntSet other)
    {
        var minPageCount = Math.Min(_pageCount, other._pageCount);
        var newCount = 0;

        // Intersect overlapping pages
        for (var i = 0; i < minPageCount; i++)
        {
            var intersected = _pages[i] & other._pages[i];
            _pages[i] = intersected;
            if (intersected != 0UL)
                newCount += PopCount(intersected);
        }

        // Clear pages beyond other's range
        for (var i = minPageCount; i < _pageCount; i++)
            _pages[i] = 0UL;

        _pageCount = minPageCount;
        _count = newCount;
    }

    private void IntersectWithOffset(IntSet other, int keyOffset)
    {
        var pageOffset = keyOffset >> PageBits;
        var bitOffset = keyOffset & PageMask;
        var newCount = 0;

        if (bitOffset == 0)
        {
            // Page-aligned intersection
            var startPage = Math.Max(0, pageOffset);
            var endPage = Math.Min(_pageCount, other._pageCount + pageOffset);

            // Clear pages before overlap
            for (var i = 0; i < startPage; i++)
                _pages[i] = 0UL;

            // Intersect overlapping pages
            for (var i = startPage; i < endPage; i++)
            {
                var otherIndex = i - pageOffset;
                var intersected = _pages[i] & other._pages[otherIndex];
                _pages[i] = intersected;
                if (intersected != 0UL)
                    newCount += PopCount(intersected);
            }

            // Clear pages after overlap
            for (var i = endPage; i < _pageCount; i++)
                _pages[i] = 0UL;
        }
        else
        {
            // Bit-shifted intersection
            var rightShift = PageSize - bitOffset;

            for (var i = 0; i < _pageCount; i++)
            {
                var otherBits = 0UL;
                var otherIndex = i - pageOffset;

                if (otherIndex >= 0 && otherIndex < other._pageCount)
                    otherBits |= other._pages[otherIndex] << bitOffset;

                if (otherIndex - 1 >= 0 && otherIndex - 1 < other._pageCount)
                    otherBits |= other._pages[otherIndex - 1] >> rightShift;

                var intersected = _pages[i] & otherBits;
                _pages[i] = intersected;
                if (intersected != 0UL)
                    newCount += PopCount(intersected);
            }
        }

        _count = newCount;
    }
    
    public void IntersectWith(IReadOnlyIntSet other)
    {
        if (other is IntSet intSet)
        {
            IntersectWith(intSet);
            return;
        }
        if (other.Count == 0 || _count == 0)
        {
            Clear();
            return;
        }
        var values = other.ToArray();
        IntersectWith(values);
    }
    public void IntersectWith(IntSet other)
    {
        if (_count == 0 || other._count == 0)
        {
            Clear();
            return;
        }

        var keyDiff = other._initialKey - _initialKey;
        if (keyDiff == 0)
        {
            // Same initial keys - direct page intersection
            IntersectWithSameKey(other);
        }
        else
        {
            // Different initial keys - need offset calculation
            IntersectWithOffset(other, keyDiff);
        }
    }

    public void ExceptWith(IntSet other)
    {
        if (_count == 0 || other._count == 0) return;

        var keyDiff = other._initialKey - _initialKey;
        if (keyDiff == 0)
        {
            // Same initial keys - direct page subtraction
            ExceptWithSameKey(other);
        }
        else
        {
            // Different initial keys - need offset calculation
            ExceptWithOffset(other, keyDiff);
        }
    }

    public void SymmetricExceptWith(IntSet other)
    {
        if (other._count == 0) return;
        if (_count == 0)
        {
            UnionWith(other);
            return;
        }

        var keyDiff = other._initialKey - _initialKey;
        if (keyDiff != 0)
        {
            if (keyDiff < 0)
            {
                // Other set has smaller initial key, shift this set
                ShiftPages(-keyDiff);
                _initialKey = other._initialKey;
            }
            else
            {
                // This set has smaller initial key, need to shift other's pages during operation
                SymmetricExceptWithOffset(other, keyDiff);
                return;
            }
        }

        // Same initial keys - direct page XOR
        SymmetricExceptWithSameKey(other);
    }
}