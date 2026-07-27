using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

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

    private IntSet intSet, intSetB;
    HashSet<int> hashSet;
    
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

        hashSet = new HashSet<int>(aKeys);
        intSet = new IntSet(aKeys);
        intSetB = new IntSet(bKeys);
    }

    
    
    [Benchmark]
    public void IntSet_Iterate()
    {
        var x = 0;
        foreach (var i in intSet)
            x += i;
    }
    
    [Benchmark]
    public void HashSet_Iterate()
    {
        var x = 0;
        foreach (var i in hashSet)
            x += i;
    }
    
    
    [Benchmark]
    public void IntSet_Add()
    {
        var idSet = new IntSet();
        for (var i = 0; i < N; i++)
        {
            idSet.Add(lookupKeys[i]);
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
    public void IntSet_Contains()
    {
        for (var i = 0; i < N; i++)
        {
            intSet.Contains(lookupKeys[i]);
        }
    }

    [Benchmark]
    public void HashSet_Contains()
    {
        for (var i = 0; i < N; i++)
        {
            hashSet.Contains(lookupKeys[i]);
        }
    }
    
    
    [Benchmark]
    public void IntSet_IntersectWith()
    {
        var idSet = new IntSet(aKeys);
        idSet.IntersectWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_IntersectWith()
    {
        var hashSet = new HashSet<int>(aKeys);
        hashSet.IntersectWith(bKeys);
    }
    
    
    [Benchmark]
    public void IntSet_UnionWith()
    {
        var idSet = new IntSet(aKeys);
        idSet.UnionWith(bKeys);
    }
    
    [Benchmark]
    public void IntSet_UnionWith_IntSet()
    {
        var idSet = new IntSet(aKeys);
        idSet.UnionWith(intSetB);
    }
    
    [Benchmark]
    public void HashSet_UnionWith()
    {
        var hashSet = new HashSet<int>(aKeys);
        hashSet.UnionWith(bKeys);
    }
    
    
    [Benchmark]
    public void IntSet_ExceptWith()
    {
        intSet.ExceptWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_ExceptWith()
    {
        hashSet.ExceptWith(bKeys);
    }
    
    
    [Benchmark]
    public void IntSet_SymmetricExceptWith()
    {
        intSet.SymmetricExceptWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_SymmetricExceptWith()
    {
        hashSet.SymmetricExceptWith(bKeys);
    }
}
