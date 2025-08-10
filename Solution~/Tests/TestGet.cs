using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestGet
{
    private Database db;
    private int employeeId;
    private int initialCapacity;
    private int departmentId;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        initialCapacity = db.EmployeeTable.Capacity;
        using var context = db.CreateContext();
        departmentId = db.DepartmentTable.Add(new Department() { name = "HR" });
        employeeId = db.EmployeeTable.Add(new Employee() { name = "Jane Smith", departmentId = departmentId});
        db.EmployeeTable.Add(new Employee() { name = "John Doe", departmentId = departmentId});
    }
    
    [Test]
    public void TestGetExisting()
    {
        var row = db.EmployeeTable.Get(employeeId);
        Assert.That(row.name, Is.EqualTo("Jane Smith"));
    }
    
    [Test]
    public void TestGetNotExisting()
    {
        Assert.Throws<KeyNotFoundException>(() => db.EmployeeTable.Get(9999));
    }
    
    [Test]
    public void TestGetAfterRemove()
    {
        var row = db.EmployeeTable.Get(employeeId);
        row.Remove();
        Assert.Throws<KeyNotFoundException>(() => db.EmployeeTable.Get(employeeId));
    }
    
  
}