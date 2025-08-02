using System.Collections.Concurrent;
using IntegrityTables;

namespace Tests.ChangeSets;

[GenerateDatabase]
public partial class Database
{
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct User
{
    [Unique]
    public string name;
    public int age;
    [Reference(typeof(Group))]
    public int groupId;
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Group
{
    [Unique]
    public string name;

    public int minAge;
}

[TestFixture]
public class TestChangeSetIsolation
{
    private Database db;
    
    [SetUp]
    public void Setup()
    {
        db = new Database();
        var adminGroupId = db.GroupTable.Add(new Group {name = "Admins"}).id;
        var userGroupId = db.GroupTable.Add(new Group {name = "Users"}).id;
        db.UserTable.Add(new User {name = "Alice", age = 30, groupId=adminGroupId});
        db.UserTable.Add(new User {name = "Bob", age = 25, groupId=userGroupId});
    }

    [Test]
    public void ConcurrentReaders_DoNotThrowOrBlock()
    {
        var exceptions = new ConcurrentBag<Exception>();
        var threads    = Enumerable.Range(0, 10)
            .Select(_ => new Thread(() =>
            {
                try
                {
                    // spin a few times to hit the lock
                    for (int i = 0; i < 100; i++)
                    {
                        // these are read-only and should run in parallel
                        var count = db.UserTable.Count;
                        var list  = db.UserTable.ToList();
                        Assert.AreEqual(2, count);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.IsEmpty(exceptions, "No reader should have thrown or been aborted.");
    }
    
    [Test]
    public void ConcurrentWritesToDifferentRows_Succeed()
    {
        // grab the two rows
        var aliceRow = db.UserTable.GetOne(r => r.name == "Alice");
        var bobRow   = db.UserTable.GetOne(r => r.name == "Bob");

        // increment Alice.age and Bob.age in parallel
        var tasks = new[]
        {
            Task.Run(() =>
            {
                var row = db.UserTable.Get(aliceRow.id);
                row.data.age += 5;
                db.UserTable.Update(ref row);
            }),
            Task.Run(() =>
            {
                var row = db.UserTable.Get(bobRow.id);
                row.data.age += 7;
                db.UserTable.Update(ref row);
            })
        };

        Task.WaitAll(tasks);

        // read back and verify
        var aliceAfter = db.UserTable.Get(aliceRow.id);
        var bobAfter   = db.UserTable.Get(bobRow.id);

        Assert.AreEqual(aliceRow.data.age + 5, aliceAfter.data.age);
        Assert.AreEqual(bobRow.data.age   + 7, bobAfter.data.age);
    }
    
    [Test]
    public void RacingWritesToSameRow_SerializeOrStaleDetect()
    {
        var original = db.UserTable.GetOne(r => r.name == "Alice");
        int successCount = 0;
        int failureCount = 0;

        // fire off N updaters all bumping Alice.age by 1
        var updaters = Enumerable.Range(0, 8)
            .Select(_ => Task.Run(() =>
            {
                try
                {
                    // each one reads, increments, then writes
                    var row = db.UserTable.Get(original.id);
                    Thread.Sleep(10);
                    row.data.age += 1;
                    db.UserTable.Update(ref row);
                    Interlocked.Increment(ref successCount);
                }
                catch (InvalidOperationException)
                {
                    // might get a stale-version error; that’s OK
                    Interlocked.Increment(ref failureCount);
                }
            }))
            .ToArray();

        Task.WaitAll(updaters);

        var after = db.UserTable.Get(original.id);
        // age should have gone up by exactly as many successful updates as we recorded
        Assert.AreEqual(original.data.age + successCount, after.data.age);
        Assert.GreaterOrEqual(successCount, 1, "At least one updater must succeed");
        Assert.GreaterOrEqual(failureCount, 1, "Some updaters should fail due to stale versio and thread.sleep");
    }
    
}