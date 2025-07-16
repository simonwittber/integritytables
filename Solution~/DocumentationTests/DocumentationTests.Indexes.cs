namespace DocumentationTests.Indexes;
using IntegrityTables;


[GenerateDatabase]
public partial class Database
{
}

// This table includes a [HotField] attribute on the lastname field, which means it will be indexed for fast scan lookups.
[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    public string firstname;
    
    [HotField]
    public string lastname;
    
    [Unique] 
    public string email;

    [Reference(typeof(Group), CollectionName = "Users")]
    public int groupId;
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Group
{
    [Unique]
    public string name;
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();

        db.UserTable.Add(new User() {firstname = "Simon", lastname = "Wittber", email = "simon@simon.com"});
        db.UserTable.Add(new User() {firstname = "Fred", lastname = "Wittber", email = "fred@simon.com"});
        db.UserTable.Add(new User() {firstname = "Boris", lastname = "Norbert", email = "boris@simon.com"});
        db.UserTable.Add(new User() {firstname = "Alice", lastname = "Smith", email = "alice@alice.com"});
        
        // The database will automatically create an index for the email field, since it is marked as [Unique].
        // We can query that field to find a user by their email address.
        var alice = db.UserTable.GetByEmail("alice@alice.com");
        Assert.That(alice.data.firstname, Is.EqualTo("Alice"));
        // There is also a TryGetByEmail method which will return false if the email does not exist in the database.
        if(db.UserTable.TryGetByEmail("boris@simon.com", out var boris))
        {
            Assert.That(boris.data.firstname, Is.EqualTo("Boris"));
        }
        
        // The HotField attribute on the lastname field means that it will be indexed for fast lookups.
        // However it is not unique, so we can have multiple users with the same lastname, which means the API
        // will return a list of user ids with that lastname.
        var userIds = db.UserTable.SelectByLastname(i => i == "Wittber");
        // note that userIds is not a List, it in an allocation free enumerator.
        foreach (var id in userIds)
        {
            var user = db.UserTable.Get(id);
            Assert.That(user.data.lastname, Is.EqualTo("Wittber"));
        }
        
        // Sometimes an API call will return an entire Row<User> instead of just the id. These are convenience methods.
        // The performance oriented API calls will return an int id instead, which is more efficient.
        
        // Foreign key fields ([Reference]) will automatically create indexes for fast lookups.
        var g = db.GroupTable.Add(new Group() { name = "Test Group" }); 
        
        // Let's add all the Wittber users to the group using a batch update API.
        // We set the groupId field of all users with the lastname "Wittber" to the id of the group we just created.
        db.UserTable.Update((ref Row<User> u) => u.data.groupId = g.id, where:(in Row<User> u) => u.data.lastname == "Wittber");
        
        // Now we can query the group to get all users in it, using a performance oriented API call.
        // Methods like this which are called from a Row<T> instance need to use a database scope. 
        // This is because each Row<T> is a struct, which does not know which database it belongs to.
        // This keeps Row<T> instances as lightweight as possible and allocation free.
        using var scope = db.CreateContext();
        var usersInGroup = g.Users();
        
        // This returns an allocation free enumerator of Row<User> which we can iterate over, which is also an observable list.
        foreach (var userId in usersInGroup)
        {
            var user = db.UserTable.Get(userId);
            Assert.That(user.data.groupId, Is.EqualTo(g.id));
            Assert.That(user.data.lastname, Is.EqualTo("Wittber"));
        }


    }
}

