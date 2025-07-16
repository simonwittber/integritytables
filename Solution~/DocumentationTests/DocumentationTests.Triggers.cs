namespace DocumentationTests.Triggers;
using IntegrityTables;


// 1. Define a class to contain your database facade. Make sure it has the [GenerateDatabase] attribute.
// The source generator wil create methods and properties in this this class based on the tables you create below...
[GenerateDatabase]
public partial class Database
{
}

// 2. Define your tables. They must be struct types. There is no need to define a primary key, this is added later.
// The email field in this struct has an [Unique] attribute. THis means that all rows in this table will have a unique value for the email field.
// The emailIsVerified field has a [Maintained] attribute, which means it cannot be modified by user code, only by triggers on the table.
// This is useful for fields that are automatically updated by the database, such as timestamps or flags, or convenient foreign keys that
// are set when a dependent field is modified
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
    [Reference(typeof(User), PropertyName = nameof(User), NotNull = true)]
    public int userId;
    public string name;
    
    // This sets up method that will run after a Character row is added to the database.
    // Notice that the Row<Character> parameter is passed by reference using the 'in' keyword.
    // This ensures that the row data is not copied, and cannot be modified in this method.
    [AfterAdd]
    public static void AfterAdd(Database db, in Row<Character> row)
    {
        // This is a trigger that will be called after a Character row is added to the database.
        // It can be used to perform additional actions, such as logging or updating other tables.
        // In this case, we send a message to the user.
        db.MessageTable.Add(new Message() { recipientId = row.data.userId, text = $"You have created a new character: {row.data.name}" });
    }
    
    [AfterRemove]
    public static void AfterRemove(Database db, in Row<Character> row)
    {
        // This is a trigger that will be called after a Character row is removed from the database.
        // In this case, we send a message to the user.
        db.MessageTable.Add(new Message() { recipientId = row.data.userId, text = $"You have removed the character: {row.data.name}" });
    }
}


[GenerateTable(typeof(Database)), Serializable]
public partial struct Message
{
    // We removed the NotNull = true parameter from the Reference attribute, so this field can be null.
    // This means that the Sender property will return a Row<User>? instead of a Row<User>.
    [Reference(typeof(User), CollectionName = "SentMessages", PropertyName = "Sender")]
    public int senderId;
    [Reference(typeof(User), CollectionName = "ReceivedMessages")]
    public int recipientId;

    public string subject;
    public string text;
    
    // This adds a trigger which will run before a Message row is added to the database.
    // This trigger will set the subject of the message to the first 32 characters of the text, followed by "..." if the subject is not set.
    // Note that the Row<Message> parameter is passed by reference using the 'ref' keyword.
    // This allows us to modify the row data before it is added to the database.
    [BeforeAdd]
    public static void BeforeAdd(Database db, ref Row<Message> row)
    {
        if (string.IsNullOrWhiteSpace(row.data.subject))
        {
            row.data.subject = $"{row.data.text.Substring(0, 32)}...";
        }
    }
    
    [BeforeUpdate]
    public static void BeforeUpdate(Database db, in Row<Message> oldRow, ref Row<Message> newRow)
    {
        // This trigger will run before a Message row is updated in the database.
        // In this case, we will not allow the subject to be changed.
        newRow.data.subject = oldRow.data.subject;
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
        
        // a Row<User> is returned, which contains the primary key of this record, and the data of the record.
        // user the .id to add character rows for this user.
        db.CharacterTable.Add(new Character() {name = "Wizard", userId = user.id});
        var fighter = db.CharacterTable.Add(new Character() {name = "Fighter", userId = user.id});
        
        // Setup a scope for the cross table database operations (the 'ReceivedMessages' method below needs this).
        using var scope = db.CreateContext();
        
        // Get a list of int id values of messages received by the user. There will two messages from the Character trigger.
        var receivedMessages = user.ReceivedMessages();
        Assert.That(receivedMessages, Is.Not.Null);
        Assert.That(receivedMessages.Count, Is.EqualTo(2));

        // Get the first message id from the list of received messages, then fetch it from the database.
        var messageId = receivedMessages[0];
        var message = db.MessageTable.Get(messageId);
        Assert.That(message.data.text, Is.EqualTo("You have created a new character: Wizard"));
        
        // The subject field of the message is set automatically by the BeforeAdd trigger.
        Assert.That(message.data.subject, Is.EqualTo("You have created a new character..."));
        
        // Remove a character, which will trigger another message.
        db.CharacterTable.Remove(in fighter);
        
        // the receivedMessages is an observable collection, so it will automatically update when the database changes.
        Assert.That(receivedMessages.Count, Is.EqualTo(3));
        // Get the last message id from the list of received messages, then fetch it from the database.
        messageId = receivedMessages[2];
        message = db.MessageTable.Get(messageId);
        Assert.That(message.data.text, Is.EqualTo("You have removed the character: Fighter"));
        
        // Lets test the Update trigger to make sure it works as expected. We should not be able to change the subject of the message.
        var oldSubject = message.data.subject;
        message.data.subject = "This should not change";
        db.MessageTable.Update(ref message);
        // The subject should not have changed, because the BeforeUpdate trigger sets it back to the original value.
        Assert.That(message.data.subject, Is.EqualTo(oldSubject));
        
        // You can hook into an observable list to get notified when the list changes.
        receivedMessages.ItemAdded += (index, newMessageId) =>
        {
            System.Console.WriteLine($"A new message was added: {newMessageId}");
        };
        
        
        

    }
}

