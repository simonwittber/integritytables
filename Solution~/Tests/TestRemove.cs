using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestRemove
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
        departmentId = db.DepartmentTable.Add(new Department() { name = "HR" });
        employeeId = db.EmployeeTable.Add(new Employee() { name = "Jane Smith", departmentId = departmentId});
        db.EmployeeTable.Add(new Employee() { name = "John Doe", departmentId = departmentId});
    }
    
    [Test]
    public void TestRemoveCountDecreases()
    {
        var id = db.EmployeeTable.Get(employeeId);
        id.Remove();
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
    }
    
    [Test]
    public void TestRemoveCapacityIsReused()
    {
        // fill the table to capacity
        while (db.EmployeeTable.Count < db.EmployeeTable.Capacity)
        {
            db.EmployeeTable.Add(new Employee() { name = "John Doe", departmentId = departmentId });
        }
        
        var id = db.EmployeeTable.Get(employeeId);
        id.Remove();
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(db.EmployeeTable.Capacity - 1));
        var capacityAfterRemove = db.EmployeeTable.Capacity;
        // adding a new employee should not increase capacity, the slot should be reused
        db.EmployeeTable.Add(new Employee() { name = "John Doe", departmentId = departmentId });
        Assert.That(db.EmployeeTable.Capacity, Is.EqualTo(capacityAfterRemove));
    }
    
    [Test]
    public void TestRemoveFailsOnReferentialIntegrity()
    {
        var row = db.DepartmentTable.Get(departmentId);
        Assert.Throws<InvalidOperationException>(() => row.Remove());
        db.EmployeeTable.Clear();
        Assert.DoesNotThrow(() => row.Remove());
    }
    
    [Test]
    public void TestClearFailsOnReferentialIntegrity()
    {
        Assert.Throws<InvalidOperationException>(() => db.DepartmentTable.Clear());
    }
}