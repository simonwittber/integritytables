namespace IntegrityTables.Tests;

[GenerateService(typeof(HumanResourcesDatabase))]
public partial class TestService : IService<HumanResourcesDatabase>
{
    public readonly List<Row<Employee>> Employees = new();

    [AfterAdd]
    public void AfterAddEmployee(in Row<Employee> row)
    {
        Employees.Add(row);
    }

    [AfterFieldUpdate(nameof(Employee.name))]
    public void AfterUpdateEmployeeName(in Row<Employee> oldRow, in Row<Employee> newRow)
    {
        Employees.Add(newRow);
    }
}

[TestFixture]
public class ServiceTests
{
    [Test]
    public void Test()
    {
        var db = new HumanResourcesDatabase();
        var service = new TestService();
        service.SetDatabase(db);
        // test service table trigger
        var employee = db.EmployeeTable.Add(new Employee());

        Assert.That(service.Employees.Count, Is.EqualTo(1));
        Assert.That(service.Employees[0].id, Is.EqualTo(employee.id));
        // test field trigger does not fire
        db.EmployeeTable.Update(ref employee);

        Assert.That(service.Employees.Count, Is.EqualTo(1));
        Assert.That(service.Employees[0].id, Is.EqualTo(employee.id));

        // test field trigger does fire
        employee.name("Something");
        db.EmployeeTable.Update(ref employee);

        Assert.That(service.Employees.Count, Is.EqualTo(2));
        Assert.That(service.Employees[0].id, Is.EqualTo(employee.id));
        Assert.That(service.Employees[1].id, Is.EqualTo(employee.id));
        Assert.That(service.Employees[1].name(), Is.EqualTo(employee.name()));

        // test that triggers get disconnected on Dispose
        service.Dispose();
        db.EmployeeTable.Add(new Employee());

        Assert.That(service.Employees.Count, Is.EqualTo(2));
    }
}