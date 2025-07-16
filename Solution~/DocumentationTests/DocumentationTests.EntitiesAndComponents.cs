// IntegrityTables implements an ECS architecutre using normal relational constructs.
// An Entity table is required.
// Each component table must implement IComponent, and have a reference to the Entity table using `int entityId`.
// If a component table has references to other component tables, the foreign key values will be automatically
// populated when the component is added to an entity by an auto generated BeforeAdd trigger.
// Apart from this, no other code is required. The API becomes:
// Add a component to an entity: `db.Add(new Component { entityId = entity.id, ... })`
// Remove a component from an entity: `db.Remove(component)`
// Query components of an entity: `db.ComponentTable.TrySelectByEntityId(entity.id, out var components)`
// or if the component has a Unique on the entityId field, you can use `db.ComponentTable.TryGet(entity.id, out var component)`.

using System.Numerics;

namespace DocumentationTests.EntitiesAndComponents;
using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

// This is the entity table. It contains no fields, as it is just a container for any component table
// to reference.
[GenerateTable(typeof(Database)), Serializable]
public partial struct Entity;

[GenerateTable(typeof(Database)), Serializable]
public partial struct Transform : IComponent
{
    // This reference field is unique, so an entity can only have one Transform component.
    // The CreateIfMissing argument means the entity record will automatically be created if it is
    // not specified when creating a Transform component.
    [Reference(typeof(Entity), PropertyName = nameof(Entity), NotNull = true, CreateIfMissing = true)]
    [Unique] public int entityId;
    
    public Vector2 position;
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Collider : IComponent
{
    // This reference field is NOT unique, so an entity can have any number of Collider components.
    // if a Collection name is specified.
    [Reference(typeof(Entity), PropertyName = nameof(Entity), NotNull = true, CollectionName = "Colliders")]
    public int entityId;
    
    // This is a reference to another component table, Transform. Because The transform table is also an IComponent,
    // and it has a unique reference to the Entity table the code generator will automatically create the necessary
    // lookup code for getting the transformId when a Collider row is created.
    // This field is also not null, so if the transform cannot be found, it will be created and attached to the entity.
    [Reference(typeof(Transform), PropertyName = nameof(Transform), NotNull = true)]
    public int transformId;
    
    public float radius;

    // This position property comes from another component, Transform.
    // We can look it up safely, because transformId is not null, so it must exist in the Transform table.
    // However it is probably better to use the Transform component directly, eg: row.Transform().position();
    public Vector2 position
    {
        get
        {
            var db = Context<Database>.Current;
            return db.TransformTable.Get(transformId).position();
        }
    }
}


public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        using var scope = db.CreateContext();
        var t = db.TransformTable.Add(new Transform { position = new Vector2(2, 3) });
        Assert.That(t.entityId(), Is.Not.Zero);
        var entityId = t.entityId();
        // Adding a component to the entity is the same as adding a row to the table.
        // No need to specify the transformId, as it is unique, and Collider is an IComponent and Transform is an IComponent the lookup
        // code is generated for us.
        var c1 = db.ColliderTable.Add(new Collider() { entityId = entityId, radius = 1 });
        var c2 = db.ColliderTable.Add(new Collider() { entityId = entityId, radius = 2 });
        
        // Check the transformId is automatically set when the Collider is added.
        Assert.That(c1.transformId(), Is.EqualTo(t.id));
        Assert.That(c1.transformId(), Is.EqualTo(t.id));

        // Check that entityId of collider and transform is equal
        Assert.That(c1.entityId(), Is.EqualTo(t.entityId()));
        Assert.That(c2.entityId(), Is.EqualTo(t.entityId()));
        
        // When checking if an entity has components of a type, use The TrySelectBy methods
        // to avoid creating a new collection.
        if (db.ColliderTableIndex.TrySelectByEntityId(entityId, out var colliders))
        {
            Assert.That(colliders, Contains.Item(c1.id));
            Assert.That(colliders, Contains.Item(c2.id));
        }
        
        // When we remove the first collider component, it will be removed from the collection.
        db.ColliderTable.Remove(c1);
        
        // No need to requery the collection, it is automatically updated as an ObservableList.
        Assert.That(colliders, Does.Not.Contain(c1.id));
        Assert.That(colliders, Contains.Item(c2.id));

    }
}

