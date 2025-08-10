using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestReferencesWithCollectionName
{
    [Test]
    public void TestPropertyNameExists()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(new Department() { name = "HR" });
        var id1 = db.EmployeeTable.Add(new Employee() { name = "John Doe", departmentId = dept });
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[dept].Contains(id1), Is.True);
        
        var id2 = db.EmployeeTable.Add(new Employee() { name = "John Blow", departmentId = dept });
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[dept].Contains(id2), Is.True);
        
        
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[0].Count, Is.EqualTo(2));
        
        var row = db.DepartmentTable.Get(dept);
        var count = 0;
        foreach (var e in row.Employees)
        {
            count++;
            Assert.That(e.name, Is.EqualTo("John Doe").Or.EqualTo("John Blow"));
        }
        Assert.That(count, Is.EqualTo(2));
    }
    
    
    
    
    
}