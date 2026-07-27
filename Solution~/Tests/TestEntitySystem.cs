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
    
    public bool AllowThreadedExecution => true;
    
    public void Execute(TransformRow transform, VelocityRow velocity)
    {
        transform.x += velocity.x;
        transform.y += velocity.y;
        transform.z += velocity.z;
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
    int[] entities;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        velocityUpdater = new TransformVelocityUpdater {database = db};
        transforms = new Transform[N];
        velocities = new Velocity[N];
        results = new Transform[N];
        entities = new int[N];
        var rng = new System.Random(123);
        for (var i = 0; i < N; i++)
        {
            var entityId = db.EntityTable.Add();
            entities[i] = entityId;
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
        velocityUpdater.Prepare();
        velocityUpdater.ExecuteScalar();
        
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
        velocityUpdater.Prepare();
        velocityUpdater.ExecuteThreaded();
        
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
