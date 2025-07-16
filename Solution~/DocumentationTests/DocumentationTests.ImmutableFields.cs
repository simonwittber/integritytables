namespace DocumentationTests.ImmutableFields;
using IntegrityTables;


[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    [Unique]
    public string email;
}

// We can use the [Immutable] attribute to mark fields that should not be modified after creation.
// This is an optimisation technique, and simplifies the generated database API.
// It is also useful for ensuring that certain fields are not changed after the object is created, which can help maintain data integrity.
// In this case, we know that senderId and recipientId should never change after a message is created, so we mark them as [Immutable].
[GenerateTable(typeof(Database)), Serializable]
public partial struct Message
{
    [Immutable]
    [Reference(typeof(User), CollectionName = "SentMessages", PropertyName = "Sender")]
    public int senderId;
    
    [Immutable]
    [Reference(typeof(User), CollectionName = "ReceivedMessages")]
    public int recipientId;
    
    public string text;
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();
        // add a new user to the database
        var user = db.UserTable.Add(new User() {email = "simon@simon.com"});
        
        // Create another user.
        var anotherUser = db.UserTable.Add(new User() {email = "boris@simon.com"});
        
        // Send a message between users.
        var msg = db.MessageTable.Add(new Message() {recipientId = user.id, senderId = anotherUser.id, text = "Hello"});

        Assert.That(msg.data.senderId, Is.EqualTo(anotherUser.id));
        
        // Try and change the senderId of the message using the flattened property methods.
        Assert.Throws<InvalidOperationException>(() =>
        {
            msg.senderId(user.id);
        });
        
        // You can still modify the senderId directly, but it will not change the value recorded in the database.
        // when extension properties become available, this will be also thrown as an InvalidOperationException.
        // Until then, the exception will occur when the database is updated.
        msg.data.senderId = user.id;
        Assert.Throws<InvalidOperationException>( () => db.MessageTable.Update(ref msg));
        
        // Assert that the senderId has not changed.
        msg = db.MessageTable.Get(msg.id);
        Assert.That(msg.data.senderId, Is.EqualTo(anotherUser.id), "The senderId should not have changed because it is marked as Immutable.");
        
        
    }
}

