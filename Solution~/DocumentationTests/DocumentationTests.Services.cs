namespace DocumentationTests.Services;
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

    public bool isLocal;
}

[GenerateTable(typeof(Database), GenerateViewModel = true), Serializable]
public partial struct Message
{
    [Reference(typeof(Player), CollectionName = "SentMessages", PropertyName = "Sender")]
    public int senderId;
    [Reference(typeof(Player), CollectionName = "ReceivedMessages", NotNull = true)]
    public int recipientId;

    public string subject;
    public string text;
}


// The [GenerateService] attribute will generate a service class for the database.
// A service class can subscribe to events across multiple tables. Services usually live for the lifetime of the application,
// and are used to handle business logic, notifications, or other operations that need to be performed when data changes.
// Compared to triggers, services are more flexible and can be used to handle complex logic that involves multiple tables or external systems.
// Regular table triggers are more lightweight and are used for simple operations keep your data model valid.
[GenerateService(typeof(Database))]
public partial class PlayerService : IService<Database>
{
    public List<PlayerViewModel> viewModels = new();
    
    private DatabaseViewModelManager viewModelManager { get; set; }

    public PlayerService(DatabaseViewModelManager viewModelManager)
    {
        this.viewModelManager = viewModelManager;
    }
    
    [AfterAdd]
    public void OnPlayerAdded(in Row<Player> player)
    {
        // When we add a local player, let's setup the ViewModel for the local UI.
        if (player.data.isLocal)
        {
            var viewModel = viewModelManager.PlayerViewModels.Get(player.id);
            // we could then send this viewModel to the UI layer. For now, lets just keep it in a list.
            viewModels.Add(viewModel);
        }
    }
    
    [AfterRemove]
    public void OnPlayerRemoved(in Row<Player> player)
    {
        if (player.data.isLocal)
        {
            // When we remove a local player, we should also remove the ViewModel from the UI layer.
            // For now, let's just remove it from our list.
            var playerId = player.id;
            viewModels.RemoveAll(vm => vm.id == playerId);
        }
    }
    
    // This trigger is different to a normal AfterUpdate trigger. It will only be called if the email field in the Player table is changed.
    [AfterFieldUpdate(nameof(Player.email))]
    public void OnPlayerUpdated(in Row<Player> oldPlayer, in Row<Player> newPlayer)
    {
        db.MessageTable.Add(new Message() { 
            recipientId = newPlayer.id, 
            text = $"Your email has been changed from {oldPlayer.data.email} to {newPlayer.data.email}." 
        });            
    }
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        var viewModelManager = new DatabaseViewModelManager(db);
        var service = new PlayerService(viewModelManager);
        service.SetDatabase(db);
        
        // add a new local player to the database
        var player = db.PlayerTable.Add(new Player() {email = "simon@simon.com", isLocal = true});
        
        // The service should already have created a ViewModel for the player.
        Assert.That(service.viewModels.Count, Is.EqualTo(1));
        Assert.That(service.viewModels[0].Email.Value, Is.EqualTo("simon@simon.com"));
        
        // change the email of the player, which should trigger a message to be added to the database.
        player.data.email = "boris@simon.com";
        db.PlayerTable.Update(ref player);
        
        // The service should have added a message to the database.
        // The Query method returns a non-allocating enumerable of rows that match the query.
        var messages = db.MessageTable.Query((in Message row) => row.recipientId == player.id);
        foreach (var message in messages)
        {
            // We should have one message with the updated email.
            Assert.That(message.data.text, Is.EqualTo("Your email has been changed from simon@simon.com to boris@simon.com."));
        }

        // Now let's remove the player, which should also remove the ViewModel from the service.
        // However, if we try and remove a player record while a message record is referencing it, we will get an error.
        Assert.Throws<InvalidOperationException>(() => db.PlayerTable.Remove(in player));
        
        // So we must remove any related table rows first, or just pass an argument to Remove to cascade the delete operation.
        // We can choose to set the recipientId of the message to 0, or remove the message entirely. In this case, we will remove the message
        // by passing CascadeOperation.Delete to the Remove method.
        db.PlayerTable.Remove(in player, CascadeOperation.Delete);
        
        // The view model should now be removed from the service as well.
        Assert.That(service.viewModels.Count, Is.EqualTo(0));



    }
}

