using System.Numerics;

namespace DocumentationTests.FieldAttributes;

using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    // This field is unique, meaning that no two rows can have the same value for this field.
    // It is also immutable, meaning that once it is set, it cannot be changed.
    [Unique, Immutable] public string email;
    
    

    // This field is a HotField, meaning it is used to quickly query rows using a table scan, but is not unique.
    [HotField] 
    public string groupName;

    // This field is a reference to another table, which allows us to create relationships between tables.
    // It is also a Computed field, which means user code cannot modify it directly,
    // but it can be set by triggers. In this case, it is set by a trigger which determines which room the user is in based on their position.
    [Computed] public int roomId;

    // This field as the [IgnoreForEquality] attribute, meaning it will not be considered when checking for equality between two User rows.
    // This is useful for fields that are not relevant for equality checks, such as temporary data or metadata
    // or even removing expensive fields from equality checks
    [IgnoreForEquality] public string magicWord;

    public Vector2 position;
    
    public float health;
    
    // This method is called when the health field is modified.
    [Validate(nameof(health))]
    public void ValidateHealth()
    {
        if (health < 0) health = 0;
        if (health > 100) health = 100;
    }

    public static partial int Compute_roomId(Database db, Row<User> row)
    {
        // This method is called to compute the roomId based on the user's position.
        // It finds the room that contains the user's position and returns its ID.
        foreach (var id in db.RoomTable)
        {
            var room = db.RoomTable.Get(id);
            if (room.data.Contains(row.position()))
            {
                return room.id;
            }
        }
        return 0; // Return 0 if no room contains the position
    }
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Room
{
    public Vector2 position;
    public Vector2 size;

    public readonly bool Contains(Vector2 vector2)
    {
        // This method checks if a given position is within the bounds of the room.
        return vector2.X >= position.X && vector2.X <= position.X + size.X &&
               vector2.Y >= position.Y && vector2.Y <= position.Y + size.Y;
    }
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        // add a new user to the database
        var user1 = db.UserTable.Add(new User() {email = "simon@simon.com", groupName = "Admin"});
        var user2 = db.UserTable.Add(new User() {email = "boris@simon.com", groupName = "Peasant"});
        
        // to use the HotField, we have a special method. It returns an enumerator which we convert to a list for easier testing.
        var adminUsers = db.UserTable.SelectByGroupName("Admin").ToList();
        Assert.That(adminUsers.Count, Is.EqualTo(1));
        Assert.That(adminUsers[0], Is.EqualTo(user1.id));
        
        // If we modify an Immutable field, it will throw an exception.
        Assert.Throws<InvalidOperationException>(() => user1.email("x@x.com"));
        
        // you can still modify the by usering the data field, however it will not be persisted to the database
        // and will throw an exception if you try to update the row.
        user1.data.email = "z@z.com";
        Assert.Throws<InvalidOperationException>(() =>
        {
            db.UserTable.Update(ref user1);
        });
        // refresh user1 from the database to get the original email back.
        user1 = db.UserTable.Get(user1.id);
        
        // add some rooms so we can test the Computed field.
        var room1 = db.RoomTable.Add(new Room() { position = new Vector2(0, 0), size = new Vector2(10, 10) });
        var room2 = db.RoomTable.Add(new Room() { position = new Vector2(50, 0), size = new Vector2(10, 10) });
        
        // The roomId will be computed based on the user's position.
        user1.data.position = new Vector2(5, 5);
        db.UserTable.Update(ref user1);
        // Check that the roomId is set correctly.
        Assert.That(user1.roomId(), Is.EqualTo(room1.id));
        
        // If we change the user's position to be outside of any room, the roomId will be set to 0.
        user1.data.position = new Vector2(100, 100);
        db.UserTable.Update(ref user1);
        Assert.That(user1.roomId(), Is.EqualTo(0));
        
        // But we cannot set the roomId directly, as it is a Computed field.
        Assert.Throws<InvalidOperationException>(() => user1.roomId(room1.id));
        
        // If we use the data field, we can set it, but it will not be persisted to the database.
        // It will also throw an exception if we try to update the row.
        user1.data.roomId = room1.id;
        Assert.Throws<InvalidOperationException>(() =>
        {
            db.UserTable.Update(ref user1);
        });
        // refresh the row with valid data from the database
        user1 = db.UserTable.Get(user1.id);
        
        // We can still use the magicWord field, but it will not be considered when checking for equality.
        var copy = user1;
        Assert.That(user1, Is.EqualTo(copy));
        
        // Changing magic word will not affect equality.
        copy.data.magicWord = "abracadabra";
        Assert.That(user1, Is.EqualTo(copy));
        
        // Changing some other field will affect equality.
        copy.data.position = new Vector2(10, 10);
        Assert.That(user1, Is.Not.EqualTo(copy));
        
        // Test an update with the ValidateHealth method
        user1.data.health = -10; // Set health to an invalid value, should be clamped to 0
        db.UserTable.Update(ref user1);
        Assert.That(user1.data.health, Is.Zero);
        
        // Test that an add uses the ValidateHealth method
        var user3 = db.UserTable.Add(new User() {email = "bert@nasa.gov", health = 101});
        // The health should be clamped to 100
        Assert.That(user3.data.health, Is.EqualTo(100));


    }
}