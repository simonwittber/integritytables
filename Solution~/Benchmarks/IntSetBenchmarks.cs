using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using IntegrityTables;

namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class IntSetBenchmarks
{
    public const int N = 100;

    private int[] aKeys, bKeys;
    private int[] lookupKeys;

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
    }

    [Benchmark]
    public void IntSet_AddAndContains()
    {
        var intSet = new IntSet();
        intSet.UnionWith(aKeys);
        
        // Contains phase
        var found = 0;
        for (var i = 0; i < N; i++)
        {
            if (intSet.Contains(lookupKeys[i]))
                found++;
        }
    }

    [Benchmark]
    public void HashSet_AddAndContains()
    {
        var hashSet = new HashSet<int>(aKeys);
        
        // Contains phase
        var found = 0;
        for (var i = 0; i < N; i++)
        {
            if (hashSet.Contains(lookupKeys[i]))
                found++;
        }
    }
    
    [Benchmark]
    public void IntSet_IntersectWith()
    {
        var intSet = new IntSet();
        intSet.UnionWith(aKeys);
        
        // IntersectWith phase
        intSet.IntersectWith(bKeys);
    }
    
    [Benchmark]
    public void HashSet_IntersectWith()
    {
        var hashSet = new HashSet<int>(aKeys);
        
        // IntersectWith phase
        hashSet.IntersectWith(bKeys);
    }
}
