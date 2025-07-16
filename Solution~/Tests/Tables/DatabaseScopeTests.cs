namespace IntegrityTables.Tests;

[TestFixture]
public class DatabaseScopeTests
{
    private HumanResourcesDatabase _db;

    [SetUp]
    public void Setup()
    {
        _db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestManyToMany()
    {
        using var scope = _db.CreateContext();

        var e1 = _db.EmployeeTable.Add(new Employee());
        var e2 = _db.EmployeeTable.Add(new Employee());
        var d1 = _db.DeskTable.Add(new Desk() { name = "Desk A" });
        var d2 = _db.DeskTable.Add(new Desk() { name = "Desk B" });
        var d3 = _db.DeskTable.Add(new Desk() { name = "Desk C" });

        e1.AddToDesks(d1);
        d2.AddToEmployees(e1);
        e2.AddToDesks(d3);
        e2.AddToDesks(d1);
        //select * from desk A inner join employee_desk B on A.id = B.desk_id where B.employee_id = e1.id
        var desks = _db.DeskTable.ManyToManyJoin(_db.EmployeeDeskTable, (in Row<Desk> D, in Row<EmployeeDesk> ED) => D.id == ED.data.desk_id && ED.data.employee_id == e1.id).ToList();
        Assert.That(desks, Contains.Item(d1));
        Assert.That(desks, Contains.Item(d2));
        Assert.That(desks, Has.Count.EqualTo(2));
        
        // use magic method
        var newDesks = e1.Desks().ToList();
        Assert.That(newDesks, Is.EquivalentTo(desks));
        
    }
    [Test]
    public void TestNotNullReferenceDoesNotReturnNullableType()
    {
        using var scope = _db.CreateContext();
        var e = _db.EmployeeTable.Add(new Employee());
        var a = _db.AddressTable.Add(new Address() {employee_id = e.id});
        var employee = a.Employee();
        Assert.That(employee, Is.TypeOf<Row<Employee>>());
        

    }

    [Test]
    public void DatabaseScope_DepartmentMethod_ReturnsCorrectDepartment()
    {
        using var scope = _db.CreateContext();
        
        var d = _db.DepartmentTable.Add(new Department() { name = "HR" });
        _db.DepartmentTable.Add(new Department() { name = "HR X" });
        var e1 = _db.EmployeeTable.Add(new Employee { name = "Alice", department_id = d.id });
        var e2 = _db.EmployeeTable.Add(new Employee { name = "Bob" });

        var department1 = e1.GetDepartment();
        Assert.That(department1, Is.Not.Null);
        var department2 = e2.GetDepartment();
        Assert.That(department2, Is.Null);
        Assert.That(department1.Value.data.name, Is.EqualTo("HR"));
    }
    
    [Test]
    public void DatabaseScope_EmployeesMethod_ReturnsCorrectEmployees()
    {
        using var scope = _db.CreateContext();
        
        var d = _db.DepartmentTable.Add(new Department() { name = "HR" });
        _db.DepartmentTable.Add(new Department() { name = "HR X" });
        var e1 = _db.EmployeeTable.Add(new Employee { name = "Alice", department_id = d.id });
        _db.EmployeeTable.Add(new Employee { name = "Bob" });

        var ems = d.GetEmployees();
        Assert.That(ems.Count, Is.EqualTo(1));
        Assert.That(ems[0], Is.EqualTo(e1.id));
    }
    
    [Test]
    public void DatabaseScope_ThrowsOutsideUsingBlock()
    {
        var d = _db.DepartmentTable.Add(new Department() {name = "HR"});
        var e1 = _db.EmployeeTable.Add(new Employee {name = "Alice", department_id = d.id});
        using (_db.CreateContext())
        {
            Assert.DoesNotThrow(() => e1.GetDepartment());
        }
        Assert.Throws<InvalidOperationException>(() => e1.GetDepartment());
    }

}

