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
public partial class TransformVelocityUpdater : ISystem
{
    public Database database { get; set; }
    
    IntSet entities = new IntSet();

    void Prepare()
    {
        entities.Clear();
        entities.UnionWith(database.TransformTable.IndexOnEntityId.Keys);
        entities.IntersectWith(database.VelocityTable.IndexOnEntityId.Keys);
        entities.Remove(-1);
    }

    public void Execute(TransformRow transform, VelocityRow velocity)
    {
        transform.x += velocity.x;
        transform.y += velocity.y;
        transform.z += velocity.z;
    }
    
    public void Execute()
    {
        Prepare();
        Threaded.ForEach(0, entities.PageCount, (int start, int end) =>
        {
            var dbTransformTable = database.TransformTable;
            var dbVelocityTable = database.VelocityTable;
            
            foreach(var i in entities.GetEnumeratorForPageRange(start, end))
            {
                var t = dbTransformTable.GetByEntityId(i);
                var v = dbVelocityTable.GetByEntityId(i);
                Execute(t, v);
            }
        });
        
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
    Transform[] results;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        velocityUpdater = new TransformVelocityUpdater {database = db};
        transforms = new Transform[N];
        velocities = new Velocity[N];
        results = new Transform[N];
        var rng = new System.Random(123);
        for (var i = 0; i < N; i++)
        {
            var entityId = db.EntityTable.Add();
            var transform = new Transform {entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle()};
            db.TransformTable.Add(entityId : entityId, x : transform.x, y : transform.y, z : transform.z);
            var velocity = new Velocity {entityId = entityId, x = rng.NextSingle(), y = rng.NextSingle(), z = rng.NextSingle()};
            db.VelocityTable.Add(entityId : entityId, x : velocity.x, y : velocity.y, z : velocity.z);
            transforms[i] = transform;
            velocities[i] = velocity;
            results[i] = new Transform {entityId = entityId, x = transform.x + velocity.x, y = transform.y + velocity.y, z = transform.z + velocity.z};
        }
    }


    [Test]
    public void ScalarTables()
    {
        var entities = new IntSet();
        var span = db.TransformTable.IndexOnEntityId.Keys;
        entities.UnionWith(span);
        Assert.That(entities.Contains(N), Is.False);
        entities.IntersectWith(db.VelocityTable.IndexOnEntityId.Keys);
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
        
        foreach(var i in entities)
        {
            var t = db.TransformTable.GetByEntityId(i);
            var r = results[i];
            Assert.That(t.x, Is.EqualTo(r.x).Within(0.0001f));
            Assert.That(t.y, Is.EqualTo(r.y).Within(0.0001f));
            Assert.That(t.z, Is.EqualTo(r.z).Within(0.0001f));

        }
    }
    
    [Test]
    public void ThreadedTables()
    {
        var entities = new IntSet();
        var span = db.TransformTable.IndexOnEntityId.Keys;
        entities.UnionWith(span);
        entities.IntersectWith(db.VelocityTable.IndexOnEntityId.Keys);
        entities.Remove(-1);
        var partition = Partitioner.Create(0, entities.PageCount);
        Threaded.ForEach(0, entities.PageCount, (int start, int end) =>
        {
            var dbTransformTable = db.TransformTable;
            var dbVelocityTable = db.VelocityTable;
            
            foreach(var i in entities.GetEnumeratorForPageRange(start, end))
            {
                var t = dbTransformTable.GetByEntityId(i);
                var v = dbVelocityTable.GetByEntityId(i);
                t.x += v.x;
                t.y += v.y;
                t.z += v.z;
            }
        });
        foreach(var i in entities)
        {
            var t = db.TransformTable.GetByEntityId(i);
            var r = results[i];
            Assert.That(t.x, Is.EqualTo(r.x).Within(0.0001f));
            Assert.That(t.y, Is.EqualTo(r.y).Within(0.0001f));
            Assert.That(t.z, Is.EqualTo(r.z).Within(0.0001f));

        }
    }

    
}
