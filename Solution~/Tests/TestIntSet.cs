using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IntegrityTables.Tests;

[TestFixture]
public class TestIntSet
{
    [SetUp]
    public void Setup()
    {
    }

    IEnumerable<(int[], int[])> TestArrays()
    {
        int[] A;
        int[] B;
        A = [12, 98, 123, 118281, 2131, 329999, 32, 1, 2, 0];
        B = [12, 1, 2, 3, 82, 11, 54, 27, 901, 324];
        yield return (A, B);
        A = [98, 12, 98, 123, 118281, 2131, 329999, 32, 1, 2, 0];
        B = [12, 1, 2, 3, 82, 11, 54, 27, 901, 324];
        yield return (A, B);
        A = [13, 12, 98, 123, 118281, 2131, 329999, 32, 1, 2, 0];
        B = [1002, 1, 2, 3, 82, 11, 54, 27, 901, 324];
        yield return (A, B);
        A = [0, 64, 128, 192, 256, 320];
        B = [0, 64];
        yield return (A, B);
        A = [1000, 2000, 3000, 4000, 5000];
        B = [1000];
        yield return (A, B);
        A = [1000, 500, 0, 500, 1000];
        B = [0, 1];
        yield return (A, B);
    }

    void Shuffle<T>(IList<T> list)
    {
        var rng = new Random(123);
        int n = list.Count;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }

    [Test]
    public void Check_Set_IsSet()
    {
        foreach (var (a, b) in TestArrays())
        {
            var bitmap = new IntSet();
            foreach(var value in a)
            {
                bitmap.Add(value);
            }
            foreach (var value in a)
            {
                Assert.That(bitmap.Contains(value), Is.True, $"Value {value} should be present in the _intSet.");
            }
            var list = bitmap.ToList();
            var hashset = new HashSet<int>(a);
            Assert.That(list, Is.EquivalentTo(hashset), 
                $"IntSet should contain the same values as the input array {string.Join(", ", a)} but contains {string.Join(", ", list)}.");
        }
    }
    
    [Test]
    public void Check_Set_UnSet()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>();
            var bitmap = new IntSet();
            foreach(var value in a)
            {
                var result = hashset.Add(value);
                var testResult = bitmap.Add(value);
                Assert.That(testResult, Is.EqualTo(result), 
                    $"Add operation for value {value} should return {result} but returned {testResult}.");
            }
            foreach (var value in b)
            {
                var result = hashset.Remove(value);
                var testResult = bitmap.Remove(value);
                Assert.That(testResult, Is.EqualTo(result), 
                    $"Remove operation for value {value} should return {result} but returned {testResult}.");
            }
            
            hashset.ExceptWith(b);
            foreach (var value in a)
            {
                Assert.That(bitmap.Contains(value), Is.EqualTo(hashset.Contains(value)), 
                    $"Value {value} should {(hashset.Contains(value) ? "" : "not ")}be present in the _intSet.");
            }

            foreach (var value in b)
            {
                Assert.That(bitmap.Contains(value), Is.False, $"Value {value} should be present in the _intSet.");
            }
        }
    }

    [Test]
    public void Check_Or_To_Bitmap()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            var bitmapB = new IntSet(b);
            
            bitmapA.UnionWith(bitmapB);
            hashset.UnionWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    [Test]
    public void Check_Or_To_Span()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            
            bitmapA.UnionWith(b);
            hashset.UnionWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    
    
    [Test]
    public void Check_Not_To_Bitmap()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            var bitmapB = new IntSet(b);
            
            bitmapA.ExceptWith(bitmapB);
            hashset.ExceptWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    [Test]
    public void Check_Not_To_Span()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            
            bitmapA.ExceptWith(b);
            hashset.ExceptWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }

    [Test]
    public void Check_And_To_Bitmap()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            var bitmapB = new IntSet(b);
            
            bitmapA.IntersectWith(bitmapB);
            hashset.IntersectWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    [Test]
    public void Check_And_To_Span()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            
            bitmapA.IntersectWith(b);
            hashset.IntersectWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    [Test]
    public void Check_Xor_To_Bitmap()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            var bitmapB = new IntSet(b);
            
            bitmapA.SymmetricExceptWith(bitmapB);
            hashset.SymmetricExceptWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }
    
    [Test]
    public void Check_Xor_To_Span()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            
            var bitmapA = new IntSet(a);
            
            bitmapA.SymmetricExceptWith(b);
            hashset.SymmetricExceptWith(b);
            
            Assert.That(bitmapA.ToArray(), Is.EquivalentTo(hashset));
            Assert.That(bitmapA.Count, Is.EqualTo(hashset.Count));
        }
    }



    [Test]
    public void CheckIterator()
    {
        foreach (var (a, b) in TestArrays())
        {
            var hashset = new HashSet<int>(a);
            var bitmapA = new IntSet(a);
            foreach (var value in bitmapA)
            {
                Assert.That(hashset.Contains(value), Is.True, $"Value {value} should be present in the _intSet.");
            }
        }
    }

}