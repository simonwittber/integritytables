using System.Collections.Concurrent;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace IntegrityTables.Benchmarks.ECS;

[GenerateDatabase]
public partial class Database
{
}
[GenerateTable(typeof(Database)), Serializable]
public partial struct Entity;

[GenerateTable(typeof(Database)), Serializable]
public partial struct Transform : IComponent
{
    [Reference(typeof(Entity)), Unique]
    public int entityId;
    public float x, y, z;
}
[GenerateTable(typeof(Database)), Serializable]
public partial struct Velocity : IComponent
{
    [Reference(typeof(Entity)), Unique]
    public int entityId;
    public float x, y, z;
}

[GenerateSystem(typeof(Database))]
public partial class TransformVelocityUpdater : ISystem<Database>
{
    public Database database { get; set; }

    public void ExecuteSoA()
    {
        entities.Clear();
        entities.UnionWith(database.TransformTable.GetEntityIdSpan());
        entities.IntersectWith(database.VelocityTable.GetEntityIdSpan());
        foreach (var entityId in entities)
        {
            database.TransformTable.InternalRows();
        }
    }
    public void ExecuteThreaded()
    {
        entities.Clear();
        entities.UnionWith(database.TransformTable.GetEntityIdSpan());
        entities.IntersectWith(database.VelocityTable.GetEntityIdSpan());
        Parallel.ForEach(entities, entityId =>
        {
            var transform = database.TransformTable.GetByEntityId(entityId);
            var velocity = database.VelocityTable.GetByEntityId(entityId);
            Execute(ref transform, in velocity);
            // database.TransformTable.Update(ref transform);
        });
    }
    
    public void Execute(ref Row<Transform> transform, in Row<Velocity> velocity)
    {
        transform.data.x += velocity.data.x;
        transform.data.y += velocity.data.y;
        transform.data.z += velocity.data.z;
    }
}

[MarkdownExporterAttribute.GitHub]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 3, iterationCount: 100)]
[MemoryDiagnoser(false)]
public class ECSBenchmarks
{

    public const int N = 100000;

    private Database db;
    public TransformVelocityUpdater velocityUpdater;
    
    [IterationSetup]
    public void Setup()
    {
        db = new Database();
        velocityUpdater = new TransformVelocityUpdater { database = db };
        var rng = new System.Random(123);
        for (var i = 0; i < N; i++)
        {
            var entityId = db.EntityTable.Add(new Entity()).id;
            db.TransformTable.Add(new Transform
                { entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle() });
            db.VelocityTable.Add(new Velocity
                { entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle() });
        }

    }
    
    [Benchmark()]
    public void ExecuteSystem()
    {
        using var scope = db.CreateContext();
        velocityUpdater.Execute();
    }
    
    [Benchmark()]
    public void ExecuteSOA()
    {
        using var scope = db.CreateContext();
        velocityUpdater.Execute();
    }
    
    [Benchmark()]
    public void ExecuteSystemParallel()
    {
        using var scope = db.CreateContext();
        velocityUpdater.ExecuteThreaded();
    }
    
}