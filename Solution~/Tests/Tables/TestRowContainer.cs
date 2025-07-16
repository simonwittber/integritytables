namespace IntegrityTables.Tests;

[TestFixture]
public class TestRowContainer
{
    private HumanResourcesDatabase db;
    
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestRowContainerIteration()
    {
        var a = db.EmployeeTable.Add(new Employee() { name = "Alice" });
        var b = db.EmployeeTable.Add(new Employee() { name = "Bob" });
        var c = db.EmployeeTable.Add(new Employee() { name = "Charlie" });

        // this bypasses all safety checks, we have raw row access using this method.
        // it also iterates backwards over the rows, so we can modify and delete them in place.
        var salary = 100;
        foreach (ref var i in db.EmployeeTable.InternalRows())
        {
            i.salary = salary;
            salary += 100;
        }
        
        // check that all salaries are updated
        a = db.EmployeeTable.Get(a.id);
        Assert.That(a.data.name, Is.EqualTo("Alice"));
        Assert.That(a.data.salary, Is.EqualTo(300));
        
        b = db.EmployeeTable.Get(b.id);
        Assert.That(b.data.name, Is.EqualTo("Bob"));
        Assert.That(b.data.salary, Is.EqualTo(200));
        
        c = db.EmployeeTable.Get(c.id);
        Assert.That(c.data.name, Is.EqualTo("Charlie"));
        Assert.That(c.data.salary, Is.EqualTo(100));
        
    }
}