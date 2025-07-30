using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IntegrityTables.Tests;

[TestFixture]
public class IdSetHashSetComparisonTests
{
    private IdSet _idSet;
    private HashSet<int> _hashSet;

    [SetUp]
    public void Setup()
    {
        _idSet = new IdSet();
        _hashSet = new HashSet<int>();
    }

    bool IsEquivalent(int[] keys, IdSet b)
    {
        foreach (var i in keys)
        {
            if (!b.Contains(i))
                return false;
        }
        return true;
    }

    [Test]
    public void CheckAddAndContains()
    {
        var values = new[] { 0, 1, 5, 10, 63, 64, 65, 127, 128, 1000, 10000 };
        
        foreach (var value in values)
        {
            var idSetResult = _idSet.Add(value);
            var hashSetResult = _hashSet.Add(value);
        }

        for (var i = 0; i < 100000; i++)
        {
            var isPresent = values.Contains(i);
            if (isPresent)
            {
                Assert.That(_idSet.Contains(i), Is.True);
            }
            else
            {
                Assert.That(_idSet.Contains(i), Is.False);
            }
        }
    }

    [Test]
    public void CheckIntersection()
    {
        var a = new[] { 0, 1, 5, 10, 63, 64, 65, 127, 128, 1000, 10000 };
        var b = new[] {0, 1, 5, 10, 12312312};
        
        _idSet.UnionWith(a);
        Assert.That(_idSet.Count, Is.EqualTo(a.Length));
        _idSet.IntersectWith(b);
        Assert.That(_idSet.Count, Is.EqualTo(4));
        Assert.That(_idSet.Contains(b[0]), Is.True);
        Assert.That(_idSet.Contains(b[1]), Is.True);
        Assert.That(_idSet.Contains(b[2]), Is.True);
        Assert.That(_idSet.Contains(b[3]), Is.True);
        Assert.That(_idSet.Contains(b[4]), Is.False);
        
        Assert.That(_idSet.Contains(a[4]), Is.False);
        Assert.That(_idSet.Contains(a[5]), Is.False);
        Assert.That(_idSet.Contains(a[6]), Is.False);
        Assert.That(_idSet.Contains(a[7]), Is.False);
        Assert.That(_idSet.Contains(a[8]), Is.False);
        Assert.That(_idSet.Contains(a[9]), Is.False);
        Assert.That(_idSet.Contains(a[10]), Is.False);
        
        _hashSet.UnionWith(a);
        _hashSet.IntersectWith(b);
        
        Assert.That(_hashSet, Is.EquivalentTo(_idSet.ToList()));
    }
    
    [Test]
    public void CheckIterator()
    {
        var values = new[] { 0, 1, 5, 10, 63, 64, 65, 127, 128, 1000, 10000 };
        
        foreach (var value in values)
        {
            var idSetResult = _idSet.Add(value);
            var hashSetResult = _hashSet.Add(value);
        }

        foreach (var value in _idSet)
        {
            Assert.That(_idSet.Contains(value), Is.True, $"Got an incorrect value from IdSet iterator: {value}");
        }
    }

    [Test]
    public void ExceptWith_EmptySpan_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3, 4, 5 };
        var exceptValues = new int[0]; // Empty array
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform except with empty collection
        _idSet.ExceptWith(exceptValues);
        _hashSet.ExceptWith(exceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void ExceptWith_NoOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3 };
        var exceptValues = new[] { 4, 5, 6 }; // No overlap
        var allTestValues = initialValues.Concat(exceptValues).ToArray();
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform except
        _idSet.ExceptWith(exceptValues);
        _hashSet.ExceptWith(exceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void ExceptWith_PartialOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3, 4, 5 };
        var exceptValues = new[] { 3, 4, 5, 6, 7 }; // 3,4,5 overlap
        var allTestValues = new[] { 1, 2, 3, 4, 5, 6, 7 };
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform except
        _idSet.ExceptWith(exceptValues);
        _hashSet.ExceptWith(exceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void ExceptWith_CompleteOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3, 4, 5 };
        var exceptValues = new[] { 1, 2, 3, 4, 5, 6, 7 }; // Complete overlap plus extras
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform except
        _idSet.ExceptWith(exceptValues);
        _hashSet.ExceptWith(exceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void SymmetricExceptWith_EmptySpan_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3, 4, 5 };
        var symmetricExceptValues = new int[0]; // Empty array
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform symmetric except with empty collection
        _idSet.SymmetricExceptWith(symmetricExceptValues);
        _hashSet.SymmetricExceptWith(symmetricExceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void SymmetricExceptWith_NoOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3 };
        var symmetricExceptValues = new[] { 4, 5, 6 }; // No overlap
        var expectedFinal = new[] { 1, 2, 3, 4, 5, 6 }; // All values should be present
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform symmetric except
        _idSet.SymmetricExceptWith(symmetricExceptValues);
        _hashSet.SymmetricExceptWith(symmetricExceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void SymmetricExceptWith_PartialOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3, 4, 5 };
        var symmetricExceptValues = new[] { 3, 4, 5, 6, 7 }; // 3,4,5 overlap
        var expectedFinal = new[] { 1, 2, 6, 7 }; // Non-overlapping elements remain
        var allTestValues = new[] { 1, 2, 3, 4, 5, 6, 7 };
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform symmetric except
        _idSet.SymmetricExceptWith(symmetricExceptValues);
        _hashSet.SymmetricExceptWith(symmetricExceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void SymmetricExceptWith_CompleteOverlap_MatchesHashSet()
    {
        var initialValues = new[] { 2, 1, 3, 4, 5 };
        var symmetricExceptValues = new[] { 1, 2, 3, 4, 5 }; // Complete overlap
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        
        // Perform symmetric except
        _idSet.SymmetricExceptWith(symmetricExceptValues);
        _hashSet.SymmetricExceptWith(symmetricExceptValues);
        
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

    }

    [Test]
    public void SymmetricExceptWith_WithDuplicates_MatchesHashSet()
    {
        var initialValues = new[] { 1, 2, 3 };
        var symmetricExceptValues = new[] { 2, 3, 3, 4, 4, 5 }; // Contains duplicates
        var expectedFinal = new[] { 1, 4, 5 }; // 2,3 removed, 4,5 added
        var allTestValues = new[] { 1, 2, 3, 4, 5 };
        
        // Setup initial state
        foreach (var value in initialValues)
        {
            _idSet.Add(value);
            _hashSet.Add(value);
        }
        Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));

        // Perform symmetric except
        _idSet.SymmetricExceptWith(symmetricExceptValues);
        _hashSet.SymmetricExceptWith(symmetricExceptValues);
        
       Assert.That(_idSet.ToList(), Is.EquivalentTo(_hashSet));
    }

   
}
