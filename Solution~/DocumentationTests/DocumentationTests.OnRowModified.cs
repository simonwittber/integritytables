namespace DocumentationTests.OnRowModified;
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

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();

        // This variable is used to test if the below callback is working correctly.
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
        
        Assert.That(userId, Is.EqualTo(user.id));
        
        // when we remove the user, the callback will set the userCopy to default.
        db.UserTable.Remove(user);
        
        Assert.That(userId, Is.EqualTo(0));
        
    }
}

