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
        departmentId = db.DepartmentTable.Add("HR");
        employeeId = db.EmployeeTable.Add(name : "Jane Smith", departmentId : departmentId);
        db.EmployeeTable.Add( name : "John Doe", departmentId : departmentId);
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