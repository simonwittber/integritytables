using System.Collections.Concurrent;
using IntegrityTables;

namespace Tests.ECS;


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
    IntSet entities = new IntSet();

    public void Prepare()
    {
        entities.Clear();
    }
}

[TestFixture]
public class TestECS
{
    public const int N = 100000;

    private Database db;
    public TransformVelocityUpdater velocityUpdater;

    Transform[] transforms;
    Velocity[] velocities;

    [SetUp]
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
            db.TransformTable.Add(entityId : entityId, x : transform.entityId, y : transform.y, z : transform.z);
            var velocity = new Velocity {entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle()};
            db.VelocityTable.Add(entityId : entityId, x : velocity.entityId, y : velocity.y, z : velocity.z);
            transforms[i] = transform;
            velocities[i] = velocity;
        }
    }

    

    [Test]
    public void ScalarTables()
    {
        var entities = new IntSet();
        var span = db.TransformTable.GetEntityIdSpan();
        entities.UnionWith(span);
        Assert.That(entities.Contains(N), Is.False);
        entities.IntersectWith(db.VelocityTable.GetEntityIdSpan());
        entities.Remove(-1);
        Assert.That(entities.Count, Is.EqualTo(N));
        Assert.That(entities.Contains(N), Is.False);
        foreach(var i in entities)
        {
            var t = db.TransformTable.GetByEntityId(i);
            var v = db.VelocityTable.GetByEntityId(i);
            t.x += v.x;
            t.y += v.y;
            t.z += v.z;
        }
    }

    
}
