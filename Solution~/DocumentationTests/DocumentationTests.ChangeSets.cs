namespace DocumentationTests.ChangeSets;
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
        // add a new user to the database
        var user = db.UserTable.Add(new User() {email = "simon@simon.com"});
        
        // ChangeSets are a way to group multiple changes to the database into a single transaction.
        // This allows you to make multiple changes to the database and then commit them all at once, or roll them back if something goes wrong.
        try
        {
            using (var changeSet = db.NewChangeSet())
            {
                // add new users to the database
                db.UserTable.Add(new User() {email = "boris@somewhere.com"});
                db.UserTable.Add(new User() {email = "alice@somewhere.com"});
                db.UserTable.Add(new User() {email = "charles@somewhere.com"});

                // Right now, these new users exist in the database, but they are not yet committed.
                // We can use the Exists method to check that these rows do exist.
                Assert.That(db.UserTable.Exists(i => i.email.EndsWith("somewhere.com")), Is.True);

                // However if we add a user with the same email, it will throw an exception because the email field is unique.
                // This will cause the entire ChangeSet to fail, and all changes will be rolled back.
                db.UserTable.Add(new User() {email = "simon@simon.com"}); // This will throw an exception
            }
        }
        catch (InvalidOperationException)
        {
            // The ChangeSet failed, and all changes were rolled back.
        }
        
        // All changes were rolled back, so the database should only contain the original user we added.
        Assert.That(db.UserTable.Exists(i => i.email.EndsWith("somewhere.com")), Is.False);

        // This time, lets add the users in a ChangeSet, but without the duplicate email.
        // We will then commit the changes, which will make them permanent in the database.
        using (var changeSet = db.NewChangeSet())
        {
            // add new users to the database
            db.UserTable.Add(new User() {email = "boris@somewhere.com"});
            db.UserTable.Add(new User() {email = "alice@somewhere.com"});
            db.UserTable.Add(new User() {email = "charles@somewhere.com"});

            // Right now, these new users exist in the database, but they are not yet committed.
            // We can use the Exists method to check that these rows do exist.
            Assert.That(db.UserTable.Exists(i => i.email.EndsWith("somewhere.com")), Is.True);

            changeSet.Commit();
        }
        
        // Now the changes have been committed,0 the database should contain the new users.
        Assert.That(db.UserTable.Exists(i => i.email.EndsWith("somewhere.com")), Is.True);
    }
}

