using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace IntegrityTables.Benchmarks.ECS;

[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database))]
public partial struct Entity;

[GenerateTable(typeof(Database))]
public partial struct Transform
{
    [Unique] public Reference<Entity> entityId;
    public float x, y, z;
}

[GenerateTable(typeof(Database))]
public partial struct Velocity
{
    [Unique] public Reference<Entity> entityId;
    public float x, y, z;
}

[GenerateSystem(typeof(Database))]
public partial class TransformVelocityUpdater : ISystem<Database>
{
    public Database database { get; set; }
    
    public bool AllowThreadedExecution => true;
    
    public void Execute(TransformRow transform, VelocityRow velocity)
    {
        transform.x += velocity.x;
        transform.y += velocity.y;
        transform.z += velocity.z;
    }
    
}

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class ECSBenchmarks
{
    [Params(100, 1000, 10000, 100000)] public int N = 100;

    private Database db;
    public TransformVelocityUpdater velocityUpdater;

    Transform[] transforms;
    Velocity[] velocities;

    [IterationSetup]
    public void Setup()
    {
        db = new Database();
        velocityUpdater = new TransformVelocityUpdater {database = db};
        transforms = new Transform[N];
        velocities = new Velocity[N];
        var rng = new System.Random(123);
        for (var i = 0; i < N; i++)
        {
            var entityId = db.EntityTable.Add();
            var transform = new Transform {entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle()};
            db.TransformTable.Add(entityId: entityId, x: transform.entityId, y: transform.y, z: transform.z);
            var velocity = new Velocity {entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle()};
            db.VelocityTable.Add(entityId: entityId, x: velocity.entityId, y: velocity.y, z: velocity.z);
            transforms[i] = transform;
            velocities[i] = velocity;
        }
    }

    [Benchmark(Baseline = true)]
    public void ScalarArrays()
    {
        for (var i = 0; i < N; i++)
        {
            var transform = transforms[i];
            var velocity = velocities[i];
            transform.x += velocity.x;
            transform.y += velocity.y;
            transform.z += velocity.z;
            transforms[i] = transform;
        }
    }

    [Benchmark]
    public void ThreadedArrays()
    {
        var partition = Partitioner.Create(0, N);
        Parallel.ForEach(partition, range =>
        {
            for (var i = range.Item1; i < range.Item2; i++)
            {
                var transform = transforms[i];
                var velocity = velocities[i];
                transform.x += velocity.x;
                transform.y += velocity.y;
                transform.z += velocity.z;
                transforms[i] = transform;
            }
        });
    }

    [Benchmark]
    public void ScalarTables()
    {
        velocityUpdater.Prepare();
        velocityUpdater.ExecuteScalar();
    }
    
    [Benchmark]
    public void ThreadedTables()
    {
        velocityUpdater.Prepare();
        velocityUpdater.ExecuteThreaded();
    }
    [Benchmark]
    
    public void AutoTables()
    {
        velocityUpdater.Execute();
    }

    
}
