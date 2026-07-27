namespace Tests;

[TestFixture]
public class TestSpan
{
    private Database db;
    private int employeeId1, employeeId2;
    private int initialCapacity;
    private int departmentId;

    [SetUp]
    public void Setup()
    {
        db = new Database();
        initialCapacity = db.EmployeeTable.Capacity;
        departmentId = db.DepartmentTable.Add(name : "HR");
        employeeId1 = db.EmployeeTable.Add(name :"Jane Smith", departmentId :departmentId);
        employeeId2 = db.EmployeeTable.Add(name :"John Doe", departmentId :departmentId);
    }
    
    [Test]
    public void TestGetSpan()
    {
        var span = db.EmployeeTable.GetIdSpan();
        Assert.That(span.Length, Is.EqualTo(2));
        Assert.That(span[0], Is.EqualTo(employeeId1));
        Assert.That(span[1], Is.EqualTo(employeeId2)); // The second
    }
    
    [Test]
    public void TestGetSpanAfterRemove()
    {
        db.EmployeeTable.Remove(employeeId1);
        var span = db.EmployeeTable.GetIdSpan();
        Assert.That(span.Length, Is.EqualTo(2));
        Assert.That(span[0], Is.EqualTo(-1));
        Assert.That(span[1], Is.EqualTo(employeeId2));
    }
    
    [Test]
    public void TestGetSpanOnReference()
    {
        var span = db.EmployeeTable.GetDepartmentIdSpan();
        Assert.That(span.Length, Is.EqualTo(2));
        Assert.That(span[0], Is.EqualTo(departmentId));
        Assert.That(span[1], Is.EqualTo(departmentId));
        
    }
    
    
    
  
}