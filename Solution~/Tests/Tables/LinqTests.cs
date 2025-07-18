namespace IntegrityTables.Tests;

using IntegrityTables.LinqExtensions;

public class LinqTests
{
    private HumanResourcesDatabase db;

    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
        db.EmployeeTable.Add(new Employee()
        {
            name = "John",
            department_id = db.DepartmentTable.Add(new Department()
            {
                name="D",
                location_id = db.LocationTable.Add(new Location() { name = "L" }).id
                
            }).id
        });
        db.EmployeeTable.Add(new Employee() { name = "Jane" });
        db.DepartmentTable.Add(new Department() {name = "E"});
    }

    [Test]
    public void TestSelect()
    {
        var employees = (from i in db.EmployeeTable select i.id).ToList();

        Assert.That(employees.Count(), Is.EqualTo(2));
    }

    [Test]
    public void TestWhereSelect()
    {
        var employees = (
            from i in db.EmployeeTable
            where i.data.name == "John"
            select i.id).ToList();
        Assert.That(employees.Count(), Is.EqualTo(1));
    }

    [Test]
    public void TestJoin()
    {
        var employees = (
            from e in db.EmployeeTable
            join d in db.DepartmentTable on e.data.department_id equals d.id 
            select (e,d)
            ).ToList();
        Assert.That(employees.Count(), Is.EqualTo(1));
        Assert.That(employees[0].e.data.name, Is.EqualTo("John"));
        Assert.That(employees[0].d.data.name, Is.EqualTo("D"));
    }
    
}