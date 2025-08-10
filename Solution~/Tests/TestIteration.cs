using System.ComponentModel.Design;
using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestIteration
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
        departmentId = db.DepartmentTable.Add( name : "HR" );
        employeeId = db.EmployeeTable.Add( name : "Jane Smith", departmentId : departmentId);
        db.EmployeeTable.Add(name : "John Doe", departmentId : departmentId);
        db.EmployeeTable.Add(name : "Alice Able", departmentId : departmentId);
    }
    
    [Test]
    public void TestForEach()
    {
        var count = 0;
        foreach (var id in db.EmployeeTable)
        {
            count += id;
            Assert.That(id, Is.GreaterThanOrEqualTo(0));
        }
        Assert.That(count, Is.EqualTo(3));
    }
    
    [Test]
    public void TestRefForEach()
    {
        var count = 0;
        foreach (ref var row in db.EmployeeTable.Rows)
        {
            count += row.id;
            Assert.That(row.id, Is.GreaterThanOrEqualTo(0));
        }        
        Assert.That(count, Is.EqualTo(3));

    }
    
    
}