using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class BitmapBenchmarks
{
    [Params(10000)] public int KeyCount = 10000;

    protected int[] aKeys, bKeys, lookupKeys, clusteredKeys;

    private HashSet<int> hashSet, hashSetB;
    private IntSet _intSet, _intSetB;
    private ClusteredBitmap clusteredBitmap;

    [IterationSetup]
    public void Setup()
    {
        aKeys = new int[KeyCount];
        bKeys = new int[KeyCount];
        lookupKeys = new int[KeyCount];
        clusteredKeys = new int[KeyCount];
        var rng = new Random(123);
        for (var i = 0; i < KeyCount; i++)
        {
            aKeys[i] = rng.Next(0, KeyCount);
            bKeys[i] = rng.Next(0, KeyCount);
            lookupKeys[i] = rng.Next(0, KeyCount);
            clusteredKeys[i] = 10000000 + rng.Next(0, KeyCount);
        }

        hashSet = new HashSet<int>(aKeys);
        hashSetB = new HashSet<int>(bKeys);
        _intSet = new IntSet(aKeys);
        _intSetB = new IntSet(bKeys);
        clusteredBitmap = new ClusteredBitmap(aKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public HashSet<int> Allocate_HashSet_with_RandomKeys()
    {
        return new HashSet<int>(aKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public IntSet Allocate_Bitmap_with_RandomKeys()
    {
        return new IntSet(aKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public ClusteredBitmap Allocate_ClusteredBitmap_with_RandomKeys()
    {
        return new ClusteredBitmap(aKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public HashSet<int> Allocate_HashSet_with_ClusteredKeys()
    {
        return new HashSet<int>(clusteredKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public IntSet Allocate_Bitmap_with_ClusteredKeys()
    {
        return new IntSet(clusteredKeys);
    }

    [BenchmarkCategory("Memory"), Benchmark(OperationsPerInvoke = 1)]
    public ClusteredBitmap Allocate_ClusteredBitmap_with_ClusteredKeys()
    {
        return new ClusteredBitmap(clusteredKeys);
    }

    [BenchmarkCategory("Contains"), Benchmark(OperationsPerInvoke = 10000)]
    public void Contains_HashSet()
    {
        for (var i = 0; i < KeyCount; i++)
            hashSet.Contains(lookupKeys[i]);
    }

    [BenchmarkCategory("Contains"), Benchmark(OperationsPerInvoke = 10000)]
    public void Contains_Bitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            _intSet.Contains(lookupKeys[i]);
    }

    [BenchmarkCategory("Contains"), Benchmark(OperationsPerInvoke = 10000)]
    public void Contains_ClusteredBitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            clusteredBitmap.IsSet(lookupKeys[i]);
    }

    [BenchmarkCategory("Add"), Benchmark(OperationsPerInvoke = 10000)]
    public void Add_HashSet()
    {
        for (var i = 0; i < KeyCount; i++)
            hashSet.Add(lookupKeys[i]);
    }

    [BenchmarkCategory("Add"), Benchmark(OperationsPerInvoke = 10000)]
    public void Add_Bitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            _intSet.Add(lookupKeys[i]);
    }

    [BenchmarkCategory("Add"), Benchmark(OperationsPerInvoke = 10000)]
    public void Add_ClusteredBitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            clusteredBitmap.Set(lookupKeys[i]);
    }

    [BenchmarkCategory("Iterate"), Benchmark(OperationsPerInvoke = 1)]
    public void Iterate_HashSet()
    {
        var x = 0;
        foreach (var i in hashSet)
            x += i;
    }

    [BenchmarkCategory("Iterate"), Benchmark(OperationsPerInvoke = 1)]
    public void Iterate_Bitmap()
    {
        var x = 0;
        foreach (var i in _intSet)
            x += i;
    }

    [BenchmarkCategory("Iterate"), Benchmark(OperationsPerInvoke = 1)]
    public void Iterate_ClusteredBitmap()
    {
        var x = 0;
        foreach (var i in clusteredBitmap)
            x += i;
    }

    [BenchmarkCategory("Remove"), Benchmark(OperationsPerInvoke = 10000)]
    public void Remove_HashSet()
    {
        for (var i = 0; i < KeyCount; i++)
            hashSet.Remove(lookupKeys[i]);
    }

    [BenchmarkCategory("Remove"), Benchmark(OperationsPerInvoke = 10000)]
    public void Remove_Bitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            _intSet.Remove(lookupKeys[i]);
    }

    [BenchmarkCategory("Remove"), Benchmark(OperationsPerInvoke = 10000)]
    public void Remove_ClusteredBitmap()
    {
        for (var i = 0; i < KeyCount; i++)
            clusteredBitmap.UnSet(lookupKeys[i]);
    }

    [BenchmarkCategory("ExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void ExceptWith_HashSet_to_Span()
    {
        hashSet.ExceptWith(bKeys);
    }

    [BenchmarkCategory("ExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void ExceptWith_Bitmap_to_Span()
    {
        _intSet.ExceptWith(bKeys);
    }

    [BenchmarkCategory("ExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void ExceptWith_ClusteredBitmap_to_Span()
    {
        clusteredBitmap.Not(bKeys);
    }

    [BenchmarkCategory("ExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void ExceptWith_HashSet_to_HashSet()
    {
        hashSet.ExceptWith(hashSetB);
    }

    [BenchmarkCategory("ExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void ExceptWith_Bitmap_to_Bitmap()
    {
        _intSet.ExceptWith(_intSetB);
    }

    [BenchmarkCategory("InterSectWith"), Benchmark(OperationsPerInvoke = 1)]
    public void IntersectWith_HashSet_to_Span()
    {
        hashSet.IntersectWith(bKeys);
    }

    [BenchmarkCategory("InterSectWith"), Benchmark(OperationsPerInvoke = 1)]
    public void IntersectWith_Bitmap_to_Span()
    {
        _intSet.UnionWith(bKeys);
    }

    [BenchmarkCategory("InterSectWith"), Benchmark(OperationsPerInvoke = 1)]
    public void IntersectWith_ClusteredBitmap_to_Span()
    {
        clusteredBitmap.And(bKeys);
    }

    [BenchmarkCategory("InterSectWith"), Benchmark(OperationsPerInvoke = 1)]
    public void IntersectWith_HashSet_to_HashSet()
    {
        hashSet.IntersectWith(hashSetB);
    }

    [BenchmarkCategory("InterSectWith"), Benchmark(OperationsPerInvoke = 1)]
    public void IntersectWith_Bitmap_to_Bitmap()
    {
        _intSet.UnionWith(_intSetB);
    }


    [BenchmarkCategory("SymmetricExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void SymmetricExceptWith_HashSet_to_Span()
    {
        hashSet.SymmetricExceptWith(bKeys);
    }

    [BenchmarkCategory("SymmetricExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void SymmetricExceptWith_Bitmap_to_Span()
    {
        _intSet.SymmetricExceptWith(bKeys);
    }

    [BenchmarkCategory("SymmetricExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void SymmetricExceptWith_ClusteredBitmap_to_Span()
    {
        clusteredBitmap.Xor(bKeys);
    }

    [BenchmarkCategory("SymmetricExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void SymmetricExceptWith_HashSet_to_HashSet()
    {
        hashSet.SymmetricExceptWith(hashSetB);
    }

    [BenchmarkCategory("SymmetricExceptWith"), Benchmark(OperationsPerInvoke = 1)]
    public void SymmetricExceptWith_Bitmap_to_Bitmap()
    {
        _intSet.SymmetricExceptWith(_intSetB);
    }


    [BenchmarkCategory("UnionWith"), Benchmark(OperationsPerInvoke = 1)]
    public void UnionWith_HashSet_to_Span()
    {
        hashSet.UnionWith(bKeys);
    }

    [BenchmarkCategory("UnionWith"), Benchmark(OperationsPerInvoke = 1)]
    public void UnionWith_Bitmap_to_Span()
    {
        _intSet.UnionWith(bKeys);
    }

    [BenchmarkCategory("UnionWith"), Benchmark(OperationsPerInvoke = 1)]
    public void UnionWith_ClusteredBitmap_to_Span()
    {
        clusteredBitmap.Or(bKeys);
    }

    [BenchmarkCategory("UnionWith"), Benchmark(OperationsPerInvoke = 1)]
    public void UnionWith_HashSet_to_HashSet()
    {
        hashSet.UnionWith(hashSetB);
    }

    [BenchmarkCategory("UnionWith"), Benchmark(OperationsPerInvoke = 1)]
    public void UnionWith_Bitmap_to_Bitmap()
    {
        _intSet.UnionWith(_intSetB);
    }
}