namespace IntegrityTables.Tests;

[TestFixture]
public class TestHotFields
{
    private HumanResourcesDatabase db;
    [SetUp]
    public void SetUp()
    {
        db = new HumanResourcesDatabase();
    }
    
    [Test]
    public void TestHotFieldQueryFuncs()
    {
        db.EmployeeTable.Add(new Employee() { salary = 10000 });
        db.EmployeeTable.Add(new Employee() { salary = 1000 }); 
        db.EmployeeTable.Add(new Employee() { salary = 20000 }); 
        db.EmployeeTable.Add(new Employee() { salary = 15000 });
        
        db.EmployeeTable.TryGetFirstBySalary(i => i > 10000, out var id);
        Assert.That(id, Is.EqualTo(3));
        db.EmployeeTable.TryGetFirstBySalary(i => i > 1000, out id);
        Assert.That(id, Is.EqualTo(1));
        Assert.That(db.EmployeeTable.TryGetFirstBySalary(i => i == 12312310, out id), Is.False);
        
        foreach(var i in db.EmployeeTable.SelectBySalary(i => i > 10000))
        {
            var row = db.EmployeeTable.Get(i);
            Assert.That(row.salary(), Is.GreaterThan(10000));
        }
    }
    
    [Test]
    public void TestHotFieldQueryValues()
    {
        db.EmployeeTable.Add(new Employee() { salary = 10000 });
        db.EmployeeTable.Add(new Employee() { salary = 1000 }); 
        db.EmployeeTable.Add(new Employee() { salary = 20000 }); 
        db.EmployeeTable.Add(new Employee() { salary = 15000 });
        db.EmployeeTable.Add(new Employee() { salary = 1000 }); 
        
        db.EmployeeTable.TryGetFirstBySalary(20000, out var id);
        Assert.That(id, Is.EqualTo(3));
        db.EmployeeTable.TryGetFirstBySalary(1000, out id);
        Assert.That(id, Is.EqualTo(2));
        Assert.That(db.EmployeeTable.TryGetFirstBySalary(12312310, out id), Is.False);
        
        foreach(var i in db.EmployeeTable.SelectBySalary(1000))
        {
            var row = db.EmployeeTable.Get(i);
            Assert.That(row.salary(), Is.EqualTo(1000));
        }
    }
}