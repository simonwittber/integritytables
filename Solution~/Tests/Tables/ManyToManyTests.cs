namespace IntegrityTables.Tests;

[TestFixture]
public class ManyToManyTests
{
    private HumanResourcesDatabase db;
    [SetUp]
    public void Setup()
    {
        db = new();
    }

    [Test]
    public void TestSymmetricAdd()
    {
        using var scope = db.CreateContext();
        var employee1 = db.EmployeeTable.Add(new Employee());
        var employee2 = db.EmployeeTable.Add(new Employee());
        employee1.AddToFriends(employee2);
        var employee1Friends = employee1.Friends().ToList();
        Assert.That(employee1Friends.Count, Is.EqualTo(1));
        var employee2Friends = employee2.Friends().ToList();
        Assert.That(employee2Friends.Count, Is.EqualTo(1));
        Assert.That(employee2Friends[0].id, Is.EqualTo(employee1.id));
        Assert.That(employee1Friends[0].id, Is.EqualTo(employee2.id));
    }
    
    [Test]
    public void TestSymmetricRemove()
    {
        using var scope = db.CreateContext();
        var employee1 = db.EmployeeTable.Add(new Employee());
        var employee2 = db.EmployeeTable.Add(new Employee());
        employee1.AddToFriends(employee2);
        employee2.RemoveFromFriends(employee1);
        var employee1Friends = employee1.Friends().ToList();
        Assert.That(employee1Friends.Count, Is.EqualTo(0));
        var employee2Friends = employee2.Friends().ToList();
        Assert.That(employee2Friends.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TestAdd()
    {
        using var scope = db.CreateContext();
        var employee1 = db.EmployeeTable.Add(new Employee());
        var desk1 = db.DeskTable.Add(new Desk() { name = "A" });
        db.DeskTable.Add(new Desk() { name = "B" });
        employee1.AddToDesks(desk1);
        var desks = employee1.Desks().ToList();
        Assert.That(desks.Count, Is.EqualTo(1));
        Assert.That(desks[0].id, Is.EqualTo(desk1.id));
    }
    
    [Test]
    public void TestRemove()
    {
        using var scope = db.CreateContext();
        var employee1 = db.EmployeeTable.Add(new Employee());
        var desk1 = db.DeskTable.Add(new Desk() { name = "A" });
        var desk2 = db.DeskTable.Add(new Desk() { name = "B" });
        employee1.AddToDesks(desk1);
        employee1.AddToDesks(desk2);
        
        employee1.RemoveFromDesks(desk1);
        var desks = employee1.Desks().ToList();
        Assert.That(desks.Count, Is.EqualTo(1));
        Assert.That(desks[0].id, Is.EqualTo(desk2.id));
    }
    
    [Test]
    public void TestRemoveInverse()
    {
        using var scope = db.CreateContext();
        var employee1 = db.EmployeeTable.Add(new Employee());
        var desk1 = db.DeskTable.Add(new Desk() { name = "A" });
        var desk2 = db.DeskTable.Add(new Desk() { name = "B" });
        employee1.AddToDesks(desk1);
        employee1.AddToDesks(desk2);
        
        desk2.RemoveFromEmployees(employee1);
        var desks = employee1.Desks().ToList();
        Assert.That(desks.Count, Is.EqualTo(1));
        Assert.That(desks[0].id, Is.EqualTo(desk1.id));
    }
}