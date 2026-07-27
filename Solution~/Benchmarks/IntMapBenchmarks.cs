using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class IntMapBenchmarks
{

    public const int N = 1000000;

    private int[] keys;
    private int[] values;
    private int[] lookup;

    private IntMap<int> map;
    private Dictionary<int, int> dict;

    [IterationSetup]
    public void Setup()
    {
        keys = new int[N];
        values = new int[N];
        lookup = new int[N];
        var rng = new Random(123);
        int RandomValue() => rng.Next(-N / 2, N / 2);
        for (var i = 0; i < N; i++)
        {
            values[i] = RandomValue();
            keys[i] = RandomValue();
            lookup[i] = RandomValue();
        }
        
        map = new IntMap<int>();
        dict = new Dictionary<int, int>();
        for (var i = 0; i < N; i++)
        {
            var key = keys[i];
            map[key] = values[i];
            dict[key] = values[i];
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void IntMap_Add()
    {
        var map = new IntMap<int>();
        for (var i = 0; i < N; i++)
        {
            var key = keys[i];
            map[key] = values[i];
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void Dict_Add()
    {
        var dict = new Dictionary<int, int>();
        for (var i = 0; i < N; i++)
        {
            var key = keys[i];
            dict[key] = values[i];
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void IntMap_Contains()
    {
        foreach(var i in lookup) 
        {
            _ = map.ContainsKey(i);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void Dict_Contains()
    {
        foreach(var i in lookup) 
        {
            _ = dict.ContainsKey(i);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void IntMap_Remove()
    {
        foreach(var i in lookup) 
        {
            _ = map.Remove(i);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void Dict_Remove()
    {
        foreach(var i in lookup) 
        {
            _ = dict.Remove(i);
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void IntMap_Iteration()
    {
        foreach(var i in map.Keys) 
        {
        }
    }
    
    [Benchmark(OperationsPerInvoke = 1000000)]
    public void Dict_Iteration()
    {
        foreach(var i in dict) 
        {
        }
    }
    
}