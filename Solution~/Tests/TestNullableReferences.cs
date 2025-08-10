using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestNullableReferences
{
    [Test]
    public void TestAddRow()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc = db.LocationTable.Add(new Location() { name = "Earth" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = loc };
        db.EmployeeTable.Add(employee);
    }
    
    [Test]
    public void TestAddRowWithInvalidReference()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc = db.LocationTable.Add(new Location() { name = "Earth" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = 9999 };
        Assert.Throws<InvalidOperationException>(()=>db.EmployeeTable.Add(employee));
    }
    
    [Test]
    public void TestAddRowAddsToIndex()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc = db.LocationTable.Add(new Location() { name = "Earth" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = loc };
        var id = db.EmployeeTable.Add(employee);
        Assert.That(db.EmployeeTable.IndexOnLocationId.ContainsKey(loc), Is.True);
    }
    
    [Test]
    public void TestSetRowAddsToIndex()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc1 = db.LocationTable.Add(new Location() { name = "Earth" });
        var loc2 = db.LocationTable.Add(new Location() { name = "Mars" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = loc1 };
        var id = db.EmployeeTable.Add(employee);
        Assert.That(db.EmployeeTable.IndexOnLocationId.ContainsKey(loc1), Is.True);
        Assert.That(db.EmployeeTable.IndexOnLocationId[loc1].Contains(id), Is.True);
        
        var row = db.EmployeeTable.Get(id);
        // set to new department
        row.locationId = loc2;
        // check that the index is updated, old value removed, new value added
        Assert.That(db.EmployeeTable.IndexOnLocationId[loc2].Contains(id), Is.True);
        Assert.That(db.EmployeeTable.IndexOnLocationId[loc1].Contains(id), Is.False);
    }
    
    [Test]
    public void TestCanSetReferenceToNull()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc = db.LocationTable.Add(new Location() { name = "Earth" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = loc };
        var id = db.EmployeeTable.Add(employee);
        var row = db.EmployeeTable.Get(id);
        Assert.DoesNotThrow(() =>  row.locationId = -1);
    }
    
    [Test]
    public void TestSetNullRemovesFromIndex()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var loc1 = db.LocationTable.Add(new Location() { name = "Earth" });
        var employee = new Employee { name = "John Doe", departmentId = dept, locationId = loc1 };
        var id = db.EmployeeTable.Add(employee);
        
        var row = db.EmployeeTable.Get(id);
        // set to new department
        row.locationId = -1;
        // check that the index is updated, old value removed, new value added
        Assert.That(db.EmployeeTable.IndexOnLocationId[loc1].Contains(id), Is.False);
    }
    
}