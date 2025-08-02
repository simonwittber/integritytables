namespace IntegrityTables.Tests.ECS;

[GenerateDatabase]
public partial class GameDatabase
{
}

[GenerateTable(typeof(GameDatabase)), Serializable]
public partial struct Entity;

[GenerateTable(typeof(GameDatabase)), Serializable]
public partial struct Transform : IComponent
{
    [Reference(typeof(Entity)), Unique]
    public int entityId;

    public float x, y, z;
}

[GenerateTable(typeof(GameDatabase)), Serializable]
public partial struct StatusEffect : IComponent
{
    [Reference(typeof(Entity))]
    public int entityId;
    public string name;
}

[GenerateTable(typeof(GameDatabase)), Serializable]
public partial struct Velocity : IComponent
{
    [Reference(typeof(Entity)), Unique]
    public int entityId;

    public float x, y, z;
}

[GenerateTable(typeof(GameDatabase)), Serializable]
public partial struct Player : IComponent
{
    [Reference(typeof(Entity)), Unique]
    public int entityId;

    public string name;
}

[GenerateSystem(typeof(GameDatabase))]
public partial class VelocitySystem : ISystem<GameDatabase>
{
    public GameDatabase database { get; set; }
    
    public void Execute(ref Row<Transform> transform, in Row<Velocity> velocity, in QueryByIdEnumerator<StatusEffect> statusEffectQuery)
    {
        transform.data.x += velocity.data.x;
        transform.data.y += velocity.data.y;
        transform.data.z += velocity.data.z;
    }
}

[TestFixture]
public class ECSTests
{
    GameDatabase db;

    [SetUp]
    public void SetUp()
    {
        db = new GameDatabase();
    }

    [Test]
    public void TestBasicSystem()
    {
        var entityId = db.EntityTable.Add(new Entity()).id;
        var transformId = db.TransformTable.Add(new Transform() { entityId = entityId }).id;
        var velocityId = db.VelocityTable.Add(new Velocity() { entityId = entityId, x = 1.0f, y = 0.0f, z = 0.0f });
        var s = new VelocitySystem();
        s.database = db;
        using var scope = db.CreateContext();
        s.Execute();
        var transform = db.TransformTable.Get(transformId);
        Assert.That(transform.x(), Is.EqualTo(1.0f));
        Assert.That(transform.y(), Is.EqualTo(0.0f));
        Assert.That(transform.z(), Is.EqualTo(0.0f));
        
    }
    
}
