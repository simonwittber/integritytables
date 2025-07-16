namespace DocumentationTests.Observers;

using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    [Unique] public string email;
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();

        // add new users to the database
        var user = db.UserTable.Add(new User() {email = "simon@simon.com"});
        var anotherUser = db.UserTable.Add(new User() {email = "boris@simon.com"});
        
        // Setup a variable and callback to observe changes to a specific row in the database.
        var emailAddress = "";
        Action<Row<User>> onUpdated = row => emailAddress = row.email();
        
        // We are going to observe a single user row.
        // When the db modifies this particular row, we will get a callback.
        // When the db modifies other rows, we will not get a callback.
        db.UserTable.AddObserver(user, onUpdated);

        // Right now the variable is still an empy string.
        Assert.That(emailAddress, Is.EqualTo(string.Empty));
        
        // When we modify the row, the callback will be called.
        user.email("bob@bob.com");
        db.UserTable.Update(ref user);
        
        // The emailAddress variable should now contain the new email address.
        Assert.That(emailAddress, Is.EqualTo("bob@bob.com"));
        
        // If we modify another row, the callback will not be called.
        anotherUser.email("x@x.com");
        db.UserTable.Update(ref anotherUser);
        // The emailAddress variable should still contain the old email address.
        Assert.That(emailAddress, Is.EqualTo("bob@bob.com"));
        
        // We can also remove the observer, so that we no longer get callbacks for this row.
        db.UserTable.RemoveObserver(user, onUpdated);
        
        // If we modify the row again, the callback will not be called.
        user.email("q@q.com");
        db.UserTable.Update(ref user);
        
        // The emailAddress variable should still contain the old email address.
        Assert.That(emailAddress, Is.EqualTo("bob@bob.com"));

    }
}