namespace IntegrityTables.Tests;

[TestFixture]
public class TableIndexTests
{
    private HumanResourcesDatabase db;
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestAdd()
    {
        var dept = db.DepartmentTable.Add(new Department() { name = "Engineering" });
        var e1 = db.EmployeeTable.Add(new Employee() { name = "Simon", department_id = dept.id});
        var employees = db.EmployeeTableIndex.SelectByDepartment_id(dept.id);
        Assert.That(employees.Count, Is.EqualTo(1));
        Assert.That(employees, Contains.Item(e1.id));
        var e2 = db.EmployeeTable.Add(new Employee() { name = "Boris", department_id = dept.id});
        employees = db.EmployeeTableIndex.SelectByDepartment_id(dept.id);
        Assert.That(employees.Count, Is.EqualTo(2));
        Assert.That(employees, Contains.Item(e1.id));
        Assert.That(employees, Contains.Item(e2.id));
    }
    
    [Test]
    public void TestUpdate()
    {
        var dept1 = db.DepartmentTable.Add(new Department() { name = "Engineering" });
        var dept2 = db.DepartmentTable.Add(new Department() { name = "HR" });

        var e1 = db.EmployeeTable.Add(new Employee() { name = "Simon", department_id = dept1.id});
        var employees1 = db.EmployeeTableIndex.SelectByDepartment_id(dept1.id);
        Assert.That(employees1, Contains.Item(e1.id));

        e1.department_id(dept2.id);
        db.EmployeeTable.Update(ref e1);
        Assert.That(employees1, Does.Not.Contain(e1.id));

        var employees2 = db.EmployeeTableIndex.SelectByDepartment_id(dept2.id);
        Assert.That(employees2, Contains.Item(e1.id));
    }
    
    [Test]
    public void TestRemove()
    {
        var dept1 = db.DepartmentTable.Add(new Department() { name = "Engineering" });

        var e1 = db.EmployeeTable.Add(new Employee() { name = "Simon", department_id = dept1.id});
        var employees1 = db.EmployeeTableIndex.SelectByDepartment_id(dept1.id);
        Assert.That(employees1, Contains.Item(e1.id));
        
        db.EmployeeTable.Remove(in e1);
        Assert.That(employees1, Does.Not.Contain(e1.id));

    }
}