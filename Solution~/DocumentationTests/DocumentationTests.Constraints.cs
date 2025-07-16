namespace DocumentationTests.Constraints;
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


[GenerateTable(typeof(Database)), Serializable]
public partial struct Message
{
    // Adding the NotNull = true parameter to the Reference attribute means that this field must always point to a valid User row.
    // Therefore, when we user the property Sender, we know it will never be null, so the Property will return a Row<User> instead of a Row<User>?
    [Reference(typeof(User), CollectionName = "SentMessages", PropertyName = "Sender", NotNull = true)]
    public int senderId;
    [Reference(typeof(User), CollectionName = "ReceivedMessages")]
    public int recipientId;
    
    public string text;
    
    [CheckConstraint]
    public static bool IsValidMessage(in Row<Message> row)
    {
        // This is a check constraint that will be enforced by the database.
        // It ensures that the sender and recipient are not the same user.
        return row.data.senderId != row.data.recipientId;
    }
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
        db.MessageTable.Add(new Message() {recipientId = user.id, senderId = anotherUser.id, text = "Hello"});
        
        // We can try and break the [Constraint] we set in the Message struct, but we will get an exception.
        Assert.Throws<InvalidOperationException>(() =>
        {
            db.MessageTable.Add(new Message() {recipientId = user.id, senderId = user.id, text = "This will not work"});
            db.MessageTable.Add(new Message() {recipientId = user.id, senderId = user.id, text = "This will not work"});
        });

        // Access the message ids sent by the user.
        // To use these collection methods, we need to tell our current scope which database we are using.
        // This is done by calling CreateScope() on the database, and lets us avoid using static variables or singletons.
        using var scope = db.CreateContext();
        
        // Get a list of int id values of messages received by the user. There will only be one.
        var receivedMessages = user.ReceivedMessages();
        Assert.That(receivedMessages, Is.Not.Null);
        Assert.That(receivedMessages.Count, Is.EqualTo(1));

        // Get the first message id from the list of received messages, then fetch it from the database.
        var messageId = receivedMessages[0];
        var message = db.MessageTable.Get(messageId);
        Assert.That(message.data.text, Is.EqualTo("Hello"));
        
        // Get the sender of the message. This will automatically look up the User row that sent the message.
        // It will not be a nullable row, because we set NotNull = true in the Reference attribute.
        var sender = message.Sender();
        Assert.That(sender.data.email , Is.EqualTo("boris@simon.com"));
        
    }
}

