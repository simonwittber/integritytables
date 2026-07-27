namespace Tests;

[TestFixture]
public class TestNotNullReferences
{
    [Test]
    public void TestAddRow()
    {
        var db = new Database();
        
        var employeeTable = db.EmployeeTable;
        Assert.That(employeeTable.Count, Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() =>  employeeTable.Add(name:"John Doe"));
        var dept = db.DepartmentTable.Add(name : "HR");
        var id1 = employeeTable.Add(name:"John Doe", departmentId :dept);
        var id2 = employeeTable.Add(name :"Jane Smith", departmentId :dept);
        Assert.That(employeeTable.Count, Is.EqualTo(2));
        Assert.IsTrue(employeeTable.ContainsKey(id1));
        Assert.IsTrue(employeeTable.ContainsKey(id2));
        var row1 = employeeTable.Get(id1);
        var row2 = employeeTable.Get(id2);
        Assert.That(row1.name, Is.EqualTo("John Doe"));
        Assert.That(row2.name, Is.EqualTo("Jane Smith"));
    }
    
    
    [Test]
    public void TestAddRowWithInvalidReference()
    {
        var db = new Database();
        
        var employeeTable = db.EmployeeTable;
        Assert.That(employeeTable.Count, Is.EqualTo(0));
        Assert.Throws<InvalidOperationException>(() =>  
            employeeTable.Add(name:"Join Doe", departmentId : 9999)
            );
    }
    
    [Test]
    public void TestAddRowAddsToIndex()
    {
        var db = new Database();
        var dept = db.DepartmentTable.Add(name : "HR");
        var id = db.EmployeeTable.Add(name:"John Doe", departmentId :dept);
        Assert.That(db.EmployeeTable.IndexOnDepartmentId.ContainsKey(dept), Is.True);
    }
    
    [Test]
    public void TestSetRowAddsToIndex()
    {
        var db = new Database();
        var employeeTable = db.EmployeeTable;
        
        var dept1 = db.DepartmentTable.Add(name : "HR");
        var dept2 = db.DepartmentTable.Add(name : "Engineering");
        var id = employeeTable.Add(name :"John Doe", departmentId :dept1);
        
        Assert.That(db.EmployeeTable.IndexOnDepartmentId.ContainsKey(dept1), Is.True);
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[dept1].Contains(id), Is.True);
        
        var row = employeeTable.Get(id);
        // set to new department
        row.departmentId = dept2;
        // check that the index is updated, old value removed, new value added
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[dept2].Contains(id), Is.True);
        Assert.That(db.EmployeeTable.IndexOnDepartmentId[dept1].Contains(id), Is.False);
    }
    
    [Test]
    public void TestCannotSetNotNullReferenceToNull()
    {
        var db = new Database();
        var dept1 = db.DepartmentTable.Add( name : "HR" );
        var id = db.EmployeeTable.Add( name : "John Doe", departmentId :dept1);
        var row = db.EmployeeTable.Get(id);
        Assert.Throws<InvalidOperationException>(() =>  row.departmentId = -1);
    }
    
}