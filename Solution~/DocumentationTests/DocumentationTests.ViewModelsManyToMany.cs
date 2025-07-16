using System.Runtime.Intrinsics.X86;

namespace DocumentationTests.ManyToManyViewModels;
using IntegrityTables;


[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database), GenerateViewModel = true), Serializable]
public partial struct Player
{
    [Unique]
    public string email;
}

[GenerateTable(typeof(Database), GenerateViewModel = true), Serializable]
public partial struct Tag
{
    [Unique]
    public string text;
}

// This is a many-to-many relationship table. It allows us to associate multiple tags with a player.
// A many to many table must only have two fields, each of which is a reference to another table.
// They must also share the same unique constraint, which is created using the [Unique] attibute with a shared name
[GenerateTable(typeof(Database), GenerateViewModel = true), Serializable]
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
[GenerateTable(typeof(Database), GenerateViewModel = true), Serializable]
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
        var viewModelManager = new DatabaseViewModelManager(db);
        // Add some players.
        var player1 = db.PlayerTable.Add(new Player() {email = "simon@simon.com" });
        var player2 = db.PlayerTable.Add(new Player() {email = "boris@simon.com" });
        
        var playerViewModel = viewModelManager.PlayerViewModels.Get(player1.id);
        
        // Add some tags.
        var tag1 = db.TagTable.Add(new Tag() {text = "newbie"});
        var tag2 = db.TagTable.Add(new Tag() {text = "pro"});
        var tag3 = db.TagTable.Add(new Tag() {text = "gamer"});
        
        using var scope = db.CreateContext();
        
        player1.AddToTags(tag1);
        
        // check that the view model now has a tag view model in its Tags collection.
        Assert.That(playerViewModel.Tags.Count, Is.EqualTo(1));
        Assert.That(playerViewModel.Tags[0].id, Is.EqualTo(tag1.id));
        
        // Check the corresponding tag view model as well
        var tagViewModel = viewModelManager.TagViewModels.Get(tag1.id);
        // They should be the same instance.
        Assert.That(playerViewModel.Tags[0], Is.SameAs(tagViewModel));
        // the tag view model should have the player in its Players collection.
        Assert.That(tagViewModel.Players.Count, Is.EqualTo(1));
        Assert.That(tagViewModel.Players[0].id, Is.EqualTo(player1.id));
        
        tag1.RemoveFromPlayers(player1); 
        
        // The tagViewModel should be removed from the player's Tags collection.
        Assert.That(playerViewModel.Tags.Count, Is.EqualTo(0));

        player1.AddToFriends(player2);
        
        // check that the player view model now has another player view model in its Friends collection.
        Assert.That(playerViewModel.Friends.Count, Is.EqualTo(1));
        Assert.That(playerViewModel.Friends[0].id, Is.EqualTo(player2.id));
        
        // Check the corresponding player view model as well
        var friendViewModel = viewModelManager.PlayerViewModels.Get(player2.id);
        // They should be the same instance.
        Assert.That(playerViewModel.Friends[0], Is.SameAs(friendViewModel));
        // the friend view model should have the player1 in its Friends collection.
        Assert.That(friendViewModel.Friends.Count, Is.EqualTo(1));
        Assert.That(friendViewModel.Friends[0].id, Is.EqualTo(player1.id));
        
        player1.RemoveFromFriends(player2);
        // The friendViewModel should be removed from the player's Friends collection.
        Assert.That(playerViewModel.Friends.Count, Is.EqualTo(0));
        // The friendViewModel should be removed from the player's Friends collection.
        Assert.That(friendViewModel.Friends.Count, Is.EqualTo(0));
        
    }
}

