namespace Tests;

[TestFixture]
public class TestTriggers
{
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
    }
    
    [Test]
    public void TestBeforeAdd()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        var log = db.EmployeeLogTable.Get(0);
        Assert.That(log.name, Is.EqualTo("Jane Smith"));
        Assert.That(log.action, Is.EqualTo("BeforeAdd"));
    }
    
    [Test]
    public void TestAfterAdd()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        // log will be BeforeAdd, BeforeNameUpdate, AfterNameUpdate, AfterAdd
        var log = db.EmployeeLogTable.Get(3);
        Assert.That(log.name, Is.EqualTo("Jane Smith"));
        Assert.That(log.action, Is.EqualTo("AfterAdd"));
    }
    
    [Test]
    public void TestBeforeRemove()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        db.EmployeeTable.Remove(employeeId);
        // log will be BeforeAdd, BeforeNameUpdate, AfterNameUpdate, AfterAdd, BeforeRemove, AfterRemove
        var log = db.EmployeeLogTable.Get(4);
        Assert.That(log.name, Is.EqualTo("Jane Smith"));
        Assert.That(log.action, Is.EqualTo("BeforeRemove"));
    }
    
    [Test]
    public void TestAfterRemove()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        db.EmployeeTable.Remove(employeeId);
        // log will be BeforeAdd, BeforeNameUpdate, AfterNameUpdate, AfterAdd, BeforeRemove, AfterRemove
        var log = db.EmployeeLogTable.Get(5);
        Assert.That(log.name, Is.EqualTo("0"));
        Assert.That(log.action, Is.EqualTo("AfterRemove"));
    }
    
    [Test]
    public void TestAfterUpdate()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        var row = db.EmployeeTable.Get(employeeId);
        row.name = "Z";
        // log will be BeforeAdd, BeforeNameUpdate, AfterNameUpdate, AfterAdd, BeforeNameUpdate, AfterNameUpdate
        Assert.That(db.EmployeeLogTable.Get(5).name, Is.EqualTo("Z"));
        Assert.That(db.EmployeeLogTable.Get(5).action, Is.EqualTo("AfterNameUpdate"));
    }
    
    [Test]
    public void TestBeforeUpdate()
    {
        var departmentId = db.DepartmentTable.Add(name : "HR");
        var employeeId = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        var row = db.EmployeeTable.Get(employeeId);
        row.name = "Z";
        // log will be BeforeAdd, BeforeNameUpdate, AfterNameUpdate, AfterAdd, BeforeNameUpdate, AfterNameUpdate
        Assert.That(db.EmployeeLogTable.Get(4).name, Is.EqualTo("Jane Smith"));
        Assert.That(db.EmployeeLogTable.Get(4).action, Is.EqualTo("BeforeNameUpdate"));
    }
    
   
  
}