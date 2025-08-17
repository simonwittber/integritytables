using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace IntegrityTables.Benchmarks;

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class DenseIdBenchmarks : IntSetBenchmarkBase
{

    [Benchmark]
    public void DenseIdMapContains()
    {
        for (var i = 0; i < N; i++) DenseIdMap.Contains(lookupKeys[i]);
    }

    [Benchmark]
    public void DenseIdMapAdd()
    {
        for (var i = 0; i < N; i++) DenseIdMap.GetOrAdd(lookupKeys[i]);
    }

    [Benchmark]
    public void DenseIdMapDenseValues_Iterate()
    {
        var x = 0;
        foreach (var i in DenseIdMap)
            x += i;
    }

    [Benchmark]
    public DenseIdMap DenseIdMapMemoryUsage()
    {
        var vm = new DenseIdMap(aKeys);
        return vm;
    }

    [Benchmark()]
    public void DenseIdMapMemoryUsage_2KeysExtremeValues()
    {
        var densemap = new DenseIdMap();
        densemap.GetOrAdd(0);
        densemap.GetOrAdd(10000000);
    }

    [Benchmark]
    public void DenseIdMapRemove()
    {
        for (var i = 0; i < N; i++) DenseIdMap.Remove(lookupKeys[i]);
    }
}