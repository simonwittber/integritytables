using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IntegrityTables;

namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class IdSetBenchmarks
{
    [Params(10, 100, 1000, 10000)]
    public int N = 1000;

    private int[] aKeys, bKeys;
    private int[] lookupKeys;

    private IdSet precomputedIdSet;
    HashSet<int> precomputedHashSet;
    
    [IterationSetup]
    public void Setup()
    {
        aKeys = new int[N];
        bKeys = new int[N];

        lookupKeys = new int[N];
        var rng = new Random(123);
        
        for (var i = 0; i < N; i++)
        {
            aKeys[i] = rng.Next(0, N);
            bKeys[i] = rng.Next(0, N); // ensure bKeys are distinct from aKeys
            lookupKeys[i] = rng.Next(0, N); // random lookup keys
        }
        
        // shuffle keys
        for (var i = 0; i < N; i++)
        {
            var j = rng.Next(i, N);
            (aKeys[i], aKeys[j]) = (aKeys[j], aKeys[i]);
            (bKeys[i], bKeys[j]) = (bKeys[j], bKeys[i]);
        }

        precomputedIdSet = new IdSet(aKeys);
        precomputedHashSet = new HashSet<int>(aKeys);
    }

    [Benchmark]
    public void IdSet_Iterate()
    {
        var x = 0;
        foreach (var i in precomputedIdSet)
            x += i;
    }
    
    [Benchmark]
    public void HashSet_Iterate()
    {
        var x = 0;
        foreach (var i in precomputedHashSet)
            x += i;
    }
    
    [Benchmark]
    public void IdSet_Add()
    {
        var intSet = new IdSet();
        for (var i = 0; i < N; i++)
        {
            intSet.Add(lookupKeys[i]);
        }
    }

    [Benchmark]
    public void HashSet_Add()
    {
        var hashSet = new HashSet<int>();
        for (var i = 0; i < N; i++)
        {
            hashSet.Add(lookupKeys[i]);
        }
    }
    
    [Benchmark]
    public void IdSet_Contains()
    {
        for (var i = 0; i < N; i++)
        {
            precomputedIdSet.Contains(lookupKeys[i]);
        }
    }

    [Benchmark]
    public void HashSet_Contains()
    {
        for (var i = 0; i < N; i++)
        {
            precomputedHashSet.Contains(lookupKeys[i]);
        }
    }
    
    [Benchmark]
    public void IdSet_IntersectWith()
    {
        var intSet = new IdSet(aKeys);
        intSet.IntersectWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_IntersectWith()
    {
        var hashSet = new HashSet<int>(aKeys);
        hashSet.IntersectWith(bKeys);
    }
    
    [Benchmark]
    public void IdSet_UnionWith()
    {
        var intSet = new IdSet(aKeys);
        intSet.UnionWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_UnionWith()
    {
        var hashSet = new HashSet<int>(aKeys);
        hashSet.UnionWith(bKeys);
    }
    
    [Benchmark]
    public void IdSet_ExceptWith()
    {
        precomputedIdSet.ExceptWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_ExceptWith()
    {
        precomputedHashSet.ExceptWith(bKeys);
    }
    
    [Benchmark]
    public void IdSet_SymmetricExceptWith()
    {
        precomputedIdSet.SymmetricExceptWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_SymmetricExceptWith()
    {
        precomputedHashSet.SymmetricExceptWith(bKeys);
    }
}
