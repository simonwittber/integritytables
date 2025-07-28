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

    HashSet<int> entities = new();
    
   
    
    public void Execute(ref Row<Transform> transform, in Row<Velocity> velocity, in QueryByIdEnumerator<StatusEffect> statusEffectQuery)
    {
        // XX
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

    
}
