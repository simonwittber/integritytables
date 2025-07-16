namespace DocumentationTests.ImmediateCallbacks;
using IntegrityTables;


// The default behavior of IntegrityTables is to defer callbacks until db.DispatchCallbacks() is called.
// However this can be changed by setting DeferCallbacks to false in the GenerateDatabase attribute.
// This will cause the callbacks to be called immediately when a row is modified, added or removed.
// All observers will also be notified immediately when a row is modified.

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

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();

        int userId = 0;
        
        // It is possible to register for a callback when a row is modified, added or removed.
        db.UserTable.OnRowModified += (index, operation) =>
        {
            // Index is the index of the row that was modified, NOT the row id.
            // Access the index using db.UserTable[index] to get the row.
            if(operation == TableOperation.Add)
                userId = db.UserTable[index].id;
            else if(operation == TableOperation.Update)
                userId = db.UserTable[index].id;
            else if (operation == TableOperation.Remove)
                userId = 0;
        };

        Assert.That(userId, Is.EqualTo(0));

        var user = db.UserTable.Add(new User() {email = "simon@simon.com"});
        
        // There is no need to call db.UserTable.DispatchCallbacks(), the callback has already been called.
        
        Assert.That(userId, Is.EqualTo(user.id));
        
    }
}

