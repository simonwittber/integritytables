using System;
using System.Linq;
using NUnit.Framework;

namespace IntegrityTables.Tests;

[TestFixture]
public class IdSetTests
{
    private IdSet _set;

    [SetUp]
    public void Setup()
    {
        _set = new IdSet();
    }

    [Test]
    public void Add_NewValue_ReturnsTrue()
    {
        Assert.That(_set.Add(42), Is.True);
        Assert.That(_set.Contains(42), Is.True);
    }

    [Test]
    public void Add_DuplicateValue_ReturnsFalse()
    {
        _set.Add(42);
        Assert.That(_set.Add(42), Is.False);
        Assert.That(_set.Contains(42), Is.True);
    }

    [Test]
    public void Contains_ExistingValue_ReturnsTrue()
    {
        _set.Add(42);
        Assert.That(_set.Contains(42), Is.True);
    }

    [Test]
    public void Contains_NonExistingValue_ReturnsFalse()
    {
        _set.Add(42);
        Assert.That(_set.Contains(43), Is.False);
    }

    [Test]
    public void Contains_EmptySet_ReturnsFalse()
    {
        Assert.That(_set.Contains(42), Is.False);
    }

    [Test]
    public void Contains_AfterMultipleOperations_WorksCorrectly()
    {
        // Add some values
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        // Remove one
        _set.Remove(2);
        
        // Add another
        _set.Add(4);
        
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.False);
        Assert.That(_set.Contains(3), Is.True);
        Assert.That(_set.Contains(4), Is.True);
    }

    [Test]
    public void Remove_ExistingValue_ReturnsTrue()
    {
        _set.Add(42);
        Assert.That(_set.Remove(42), Is.True);
        Assert.That(_set.Contains(42), Is.False);
    }

    [Test]
    public void Remove_NonExistingValue_ReturnsFalse()
    {
        _set.Add(42);
        Assert.That(_set.Remove(43), Is.False);
        Assert.That(_set.Contains(42), Is.True);
    }

    [Test]
    public void Remove_FromEmptySet_ReturnsFalse()
    {
        Assert.That(_set.Remove(42), Is.False);
    }

    [Test]
    public void Remove_AllElements_SetBecomesEmpty()
    {
        var values = new[] { 1, 2, 3, 4, 5 };
        
        // Add all values
        foreach (var value in values)
        {
            _set.Add(value);
        }
        
        // Remove all values
        foreach (var value in values)
        {
            Assert.That(_set.Remove(value), Is.True, $"Failed to remove {value}");
        }
        
        // Verify set is empty
        foreach (var value in values)
        {
            Assert.That(_set.Contains(value), Is.False, $"Set still contains {value}");
        }
    }

    [Test]
    public void AddAndRemove_SameValueMultipleTimes_WorksCorrectly()
    {
        const int value = 42;
        
        // Add -> Remove -> Add -> Remove cycle
        Assert.That(_set.Add(value), Is.True);
        Assert.That(_set.Contains(value), Is.True);
        
        Assert.That(_set.Remove(value), Is.True);
        Assert.That(_set.Contains(value), Is.False);
        
        Assert.That(_set.Add(value), Is.True);
        Assert.That(_set.Contains(value), Is.True);
        
        Assert.That(_set.Remove(value), Is.True);
        Assert.That(_set.Contains(value), Is.False);
    }

    [Test]
    public void IntersectWith_EmptySpan_RemovesAllElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        _set.IntersectWith(Span<int>.Empty);
        
        Assert.That(_set.Contains(1), Is.False);
        Assert.That(_set.Contains(2), Is.False);
        Assert.That(_set.Contains(3), Is.False);
    }

    [Test]
    public void IntersectWith_OverlappingValues_KeepsOnlyCommonElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        _set.Add(4);
        _set.Add(5);
        
        var intersectValues = new int[] { 2, 4, 6, 8 };
        _set.IntersectWith(intersectValues);
        
        Assert.That(_set.Contains(1), Is.False);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.False);
        Assert.That(_set.Contains(4), Is.True);
        Assert.That(_set.Contains(5), Is.False);
        Assert.That(_set.Contains(6), Is.False); // Not originally in set
    }

    [Test]
    public void IntersectWith_NoOverlappingValues_RemovesAllElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        var intersectValues = new int[] { 4, 5, 6 };
        _set.IntersectWith(intersectValues);
        
        Assert.That(_set.Contains(1), Is.False);
        Assert.That(_set.Contains(2), Is.False);
        Assert.That(_set.Contains(3), Is.False);
    }

    [Test]
    public void IntersectWith_IdenticalValues_KeepsAllElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        var intersectValues = new int[] { 1, 2, 3 };
        _set.IntersectWith(intersectValues);
        
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.True);
    }

    [Test]
    public void IntersectWith_EmptySet_HasNoEffect()
    {
        var intersectValues = new int[] { 1, 2, 3 };
        _set.IntersectWith(intersectValues);
        
        // Set should remain empty
        Assert.That(_set.Contains(1), Is.False);
        Assert.That(_set.Contains(2), Is.False);
        Assert.That(_set.Contains(3), Is.False);
    }

    [Test]
    public void IntersectWith_LargeSpan_PerformanceTest()
    {
        // Add elements to set
        for (int i = 0; i < 1000; i += 2) // Even numbers
        {
            _set.Add(i);
        }
        
        // Create large span with overlapping values
        var largeSpan = Enumerable.Range(0, 2000).Where(x => x % 3 == 0).ToArray();
        
        _set.IntersectWith(largeSpan);
        
        // Verify intersection worked correctly
        // Should contain numbers that are both even (original set) and divisible by 3 (span)
        // i.e., numbers divisible by 6
        for (int i = 0; i < 1000; i++)
        {
            bool shouldContain = i % 6 == 0;
            Assert.That(_set.Contains(i), Is.EqualTo(shouldContain), 
                $"Element {i} presence mismatch. Should contain: {shouldContain}");
        }
    }

   

    [Test]
    public void HashCollisions_HandledCorrectly()
    {
        // This test assumes that some values will hash to the same bucket
        // and tests that linear probing works correctly
        
        // Add many values to increase chance of collisions
        var values = Enumerable.Range(0, 100).ToArray();
        
        foreach (var value in values)
        {
            Assert.That(_set.Add(value), Is.True, $"Failed to add {value}");
        }
        
        // Verify all values are present
        foreach (var value in values)
        {
            Assert.That(_set.Contains(value), Is.True, $"Missing value {value}");
        }
        
        // Remove every other value
        for (int i = 0; i < values.Length; i += 2)
        {
            Assert.That(_set.Remove(values[i]), Is.True, $"Failed to remove {values[i]}");
        }
        
        // Verify removal worked correctly
        for (int i = 0; i < values.Length; i++)
        {
            bool shouldExist = i % 2 == 1;
            Assert.That(_set.Contains(values[i]), Is.EqualTo(shouldExist), 
                $"Value {values[i]} existence mismatch after removal");
        }
    }

    [Test]
    public void Zero_HandledCorrectly()
    {
        // Zero is a special case that should be handled correctly
        Assert.That(_set.Add(0), Is.True);
        Assert.That(_set.Contains(0), Is.True);
        Assert.That(_set.Add(0), Is.False); // Duplicate
        Assert.That(_set.Remove(0), Is.True);
        Assert.That(_set.Contains(0), Is.False);
        Assert.That(_set.Remove(0), Is.False); // Already removed
    }


    #region UnionWith Tests

    [Test]
    public void UnionWith_EmptySpan_NoChange()
    {
        _set.Add(1);
        _set.Add(2);
        
        _set.UnionWith(Span<int>.Empty);
        
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
    }

    [Test]
    public void UnionWith_EmptySet_AddsAllElements()
    {
        var values = new int[] { 1, 2, 3, 4, 5 };
        
        _set.UnionWith(values);
        
        foreach (var value in values)
        {
            Assert.That(_set.Contains(value), Is.True, $"Set doesn't contain {value}");
        }
    }

    [Test]
    public void UnionWith_DisjointSets_AddsAllNewElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        var newValues = new int[] { 4, 5, 6 };
        _set.UnionWith(newValues);
        
        // Check original elements still exist
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.True);
        
        // Check new elements were added
        Assert.That(_set.Contains(4), Is.True);
        Assert.That(_set.Contains(5), Is.True);
        Assert.That(_set.Contains(6), Is.True);
    }

    [Test]
    public void UnionWith_OverlappingSets_AddsOnlyNewElements()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        var unionValues = new int[] { 2, 3, 4, 5 }; // 2,3 overlap, 4,5 are new
        _set.UnionWith(unionValues);
        
        // All elements should be present
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.True);
        Assert.That(_set.Contains(4), Is.True);
        Assert.That(_set.Contains(5), Is.True);
    }

    [Test]
    public void UnionWith_IdenticalSets_NoChange()
    {
        _set.Add(1);
        _set.Add(2);
        _set.Add(3);
        
        var identicalValues = new int[] { 1, 2, 3 };
        _set.UnionWith(identicalValues);
        
        // All original elements should still be present
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.True);
        
        // No unexpected elements should be added
        Assert.That(_set.Contains(4), Is.False);
    }

    [Test]
    public void UnionWith_DuplicatesInSpan_HandledCorrectly()
    {
        _set.Add(1);
        
        var valuesWithDuplicates = new int[] { 1, 2, 2, 3, 3, 3 };
        _set.UnionWith(valuesWithDuplicates);
        
        Assert.That(_set.Contains(1), Is.True);
        Assert.That(_set.Contains(2), Is.True);
        Assert.That(_set.Contains(3), Is.True);
        Assert.That(_set.Contains(4), Is.False);
    }

 

    [Test]
    public void UnionWith_TriggersResize_WorksCorrectly()
    {
        // Start with small set
        _set.Add(1);
        _set.Add(2);
        
        // Union with large span that should trigger resize
        var largeSpan = Enumerable.Range(3, 50).ToArray();
        _set.UnionWith(largeSpan);
        
        // Verify all elements are present after potential resize
        for (int i = 1; i <= 52; i++)
        {
            Assert.That(_set.Contains(i), Is.True, $"Set doesn't contain {i} after resize");
        }
    }

    [Test]
    public void UnionWith_AfterRemovalOperations_WorksCorrectly()
    {
        // Add initial elements
        for (int i = 1; i <= 10; i++)
        {
            _set.Add(i);
        }
        
        // Remove some elements
        _set.Remove(2);
        _set.Remove(4);
        _set.Remove(6);
        
        // Union with new elements
        var newValues = new int[] { 11, 12, 13, 2 }; // Include one previously removed value
        _set.UnionWith(newValues);
        
        // Verify final state
        var expectedPresent = new[] { 1, 2, 3, 5, 7, 8, 9, 10, 11, 12, 13 };
        var expectedAbsent = new[] { 4, 6, 14, 15 };
        
        foreach (var value in expectedPresent)
        {
            Assert.That(_set.Contains(value), Is.True, $"Set should contain {value}");
        }
        
        foreach (var value in expectedAbsent)
        {
            Assert.That(_set.Contains(value), Is.False, $"Set should not contain {value}");
        }
    }

    #endregion
}
