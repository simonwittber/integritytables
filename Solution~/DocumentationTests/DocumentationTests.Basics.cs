namespace DocumentationTests.Basics;
using IntegrityTables;


// 1. Define a class to contain your database facade. Make sure it has the [GenerateDatabase] attribute.
// The source generator wil create methods and properties in this this class based on the tables you create below...
[GenerateDatabase]
public partial class Database
{
}

// 2. Define your tables. They must be struct types. There is no need to define a primary key, this is added later.
// The email field in this struct has an [Unique] attribute. THis means that all rows in this table will have a unique value for the email field.
[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    [Unique]
    public string email;
}

// This table has a field which has a [Reference] attribute. This means the field is a reference to another table.
// It must be an int, and the value of 0 in this field means the field is "null" or not pointing to any row in the referenced table.
[GenerateTable(typeof(Database)), Serializable]
public partial struct Character
{
    [Reference(typeof(User))]
    public int userId;
    public string name;
}


// This table specified a collection name for two fields. A collection name is the name used in the referenced table to refer to 
// rows in this table. In this case, a row in the User table will have a collection of 'SentMessages' which will contain all rows
// in this table where senderId == user.id. There will also be a collection of 'ReceivedMessages' which will contain all rows
// in this table where recipientId == user.id
// The PropertyName in the [Reference] attribute will be used to create a special property on the Message row called 'Sender' which
// will automatically look up the User row that this message was sent by.
[GenerateTable(typeof(Database)), Serializable]
public partial struct Message
{
    [Reference(typeof(User), CollectionName = "SentMessages", PropertyName = "Sender")]
    public int senderId;
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
        
        // a Row<User> is returned, which contains the primary key of this record, and the data of the record.
        // user the .id to add character rows for this user.
        db.CharacterTable.Add(new Character() {name = "Wizard", userId = user.id});
        db.CharacterTable.Add(new Character() {name = "Fighter", userId = user.id});

        // Access the characters for the user
        var characters = db.CharacterTable.Query((in Character row) => row.userId == user.id);
        foreach (var i in characters)
        {
        }
        
        // Create another user.
        var anotherUser = db.UserTable.Add(new User() {email = "boris@simon.com"});
        
        // We cannot create a record with invalid foreign key references.
        Assert.Throws<InvalidOperationException>(() => db.MessageTable.Add(new Message() {recipientId = user.id, senderId = 123, text = "Hello"}));
        
        // Send a message between users.
        db.MessageTable.Add(new Message() {recipientId = user.id, senderId = anotherUser.id, text = "Hello"});

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
        var sender = message.Sender();
        
        // because the senderId might be 0, we need to check if the sender is null before getting the actual row record.
        if (sender.HasValue)
        {
            Assert.That(sender.Value.data.email , Is.EqualTo("boris@simon.com"));
        }
    }
}

