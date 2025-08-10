using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestReferencesWithPropertyName
{
    [Test]
    public void TestPropertyNameExists()
    {
        var db = new Database();
        var employee = new Employee { name = "John Doe" };
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        employee.departmentId = dept;
        var id = db.EmployeeTable.Add(employee);
        var row = db.EmployeeTable.Get(id);
        Assert.That((int)row.departmentId, Is.EqualTo(dept));
        var deptRow = row.Department;
        Assert.That(deptRow.name, Is.EqualTo("HR"));
    }
    
    [Test]
    public void TestPropertyNameNotExists()
    {
        var db = new Database();
        var employee = new Employee { name = "John Doe" };
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        employee.departmentId = dept;
        var id = db.EmployeeTable.Add(employee);
        var row = db.EmployeeTable.Get(id);
        
        Assert.Throws<KeyNotFoundException>(() =>
        {
            var location = row.Location;
        });
    }
    
    
    
}