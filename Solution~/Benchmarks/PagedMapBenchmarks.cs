using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser]
public class PagedMapBenchmarks
{

    public const int N = 1000000;

    
    private int[] keys, smallKeys;
    private int[] values;

    [IterationSetup]
    public void Setup()
    {
        
        keys = new int[N];
        values = new int[N];
        smallKeys = new int[N];
        var rng = new Random(123);
        for (var i = 0; i < N; i++)
        {
            values[i] = i + 1;
            keys[i] = i;
            smallKeys[i] = i % (N / 10000); // reduce the key space to match the use case of PagedMap
        }
        // shuffle  keys
        for (var i = 0; i < N; i++)
        {
            var j = rng.Next(i, N);
            (keys[i], keys[j]) = (keys[j], keys[i]);
            (values[i], values[j]) = (values[j], values[i]);
            (smallKeys[i], smallKeys[j]) = (smallKeys[j], smallKeys[i]);
        }
    }
    
    [Benchmark()]
    public void PagedAddGet()
    {
        var _paged = new PagedMap();
        for (var i = 0; i < N; i++)
        {
            // we use a smaller set of keys, as PagedMap is designed to optimize it's memory usage
            // for a smaller range of keys, so we use keys % (N/10000) to reduce the key space
            // as compared to the full range of keys which muse be used with IdMap and Dictionary
            var key = smallKeys[i];
            _paged[key] = values[i];
            values[i] = _paged[key];
        }
    }
    
    [Benchmark()]
    public void IdMapAddGet()
    {
        var _idMap = new IdMap();
        for (var i = 0; i < N; i++)
        {
            var key = keys[i];
            _idMap[key] = values[i];
            values[i] = _idMap[key];
        }
    }

    
    [Benchmark()]
    public void DictAddGetVsIdMap()
    {
        var _dict = new Dictionary<int, int>();
        for (var i = 0; i < N; i++)
        {
            var key = keys[i];
            _dict[key] = values[i];
            values[i] = _dict[key];
        }
    }
    
    [Benchmark()]
    public void DictAddGetVsPagedMap()
    {
        var _dict = new Dictionary<int, int>();
        for (var i = 0; i < N; i++)
        {
            var key = smallKeys[i];
            _dict[key] = values[i];
            values[i] = _dict[key];
        }
    }
    
}