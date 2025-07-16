namespace DocumentationTests.ManyToMany;
using IntegrityTables;


[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Player
{
    [Unique]
    public string email;
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Tag
{
    [Unique]
    public string text;
}

// This is a many-to-many relationship table. It allows us to associate multiple tags with a player.
// A many to many table must only have two fields, each of which is a reference to another table.
// They must also share the same unique constraint, which is created using the [Unique] attibute with a shared name
[GenerateTable(typeof(Database)), Serializable]
public partial struct PlayerTag
{
    [Unique("PlayerTagUniqueConstraint")] // required for many-to-many relationships
    [Reference(typeof(Player), CollectionName = "Tags", NotNull = true)]
    public int playerId;

    [Unique("PlayerTagUniqueConstraint")] // required for many-to-many relationships
    [Reference(typeof(Tag), CollectionName = "Players", NotNull = true)]
    public int tagId;
}

// This table is also used to create a many-to-many relationship, but it is used to associate players with other players.
// Because bot [Reference] fields point to the same type (Player), and have the same CollectionName, this is a symmetric many-to-many relationship.
// A constraint and trigger will be addded to ensure that the key pairs are ordered, to avoid duplicates.
[GenerateTable(typeof(Database)), Serializable]
public partial struct Friendship
{
    [Unique("PlayerTagUniqueConstraint")] // required for many-to-many relationships
    [Reference(typeof(Player), CollectionName = "Friends", NotNull = true)]
    public int playerAId;
    [Unique("PlayerTagUniqueConstraint")] // required for many-to-many relationships
    [Reference(typeof(Player), CollectionName = "Friends", NotNull = true)]
    public int playerBId;
}


public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        
        // Add some players.
        var player1 = db.PlayerTable.Add(new Player() {email = "simon@simon.com" });
        var player2 = db.PlayerTable.Add(new Player() {email = "boris@simon.com" });
        
        // Add some tags.
        var tag1 = db.TagTable.Add(new Tag() {text = "newbie"});
        var tag2 = db.TagTable.Add(new Tag() {text = "pro"});
        var tag3 = db.TagTable.Add(new Tag() {text = "gamer"});
        
        using var scope = db.CreateContext();
        
        // Players and Tags are separate tables, but we can associate them using the PlayerTag table.
        // This allows the generator to create an API that allows us to easily access the tags for a player, and the players for a tag.
        player1.AddToTags(tag1);
        
        // player1 should now exist in the Tags.Players collection
        // we convert the non allocating enumerable to a list for easier testing.
        var players = tag1.Players().ToList();
        Assert.That(players.Count, Is.EqualTo(1));
        Assert.That(players[0].id, Is.EqualTo(player1.id));
        
        // We can also remove the player from the tag, from either side of the relationship.
        tag1.RemoveFromPlayers(player1); // or player1.RemoveFromTags(tag1); 
        
        // Let's check the result from the other side of the relationship.
        var tags = player1.Tags().ToList();
        Assert.That(tags.Count, Is.EqualTo(0));


        player1.AddToFriends(player2);
        // player1 should now exist in the Player.Friends collection of Player2, and vice versa.
        
        var friendsOfPlayer1 = player1.Friends().ToList();
        Assert.That(friendsOfPlayer1.Count, Is.EqualTo(1));
        Assert.That(friendsOfPlayer1[0].id, Is.EqualTo(player2.id));
        
        var friendsOfPlayer2 = player2.Friends().ToList();
        Assert.That(friendsOfPlayer2.Count, Is.EqualTo(1));
        Assert.That(friendsOfPlayer2[0].id, Is.EqualTo(player1.id));
    }
}

