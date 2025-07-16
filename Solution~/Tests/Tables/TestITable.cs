namespace IntegrityTables.Tests;

[TestFixture]
public class TestITable
{
    private HumanResourcesDatabase db;


    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
        using var scope = db.CreateContext();
        var d1 = db.DepartmentTable.Add(new Department() {name = "Engineering"});
        var d2 = db.DepartmentTable.Add(new Department() {name = "Marketing"});
        var e1 = db.EmployeeTable.Add(new Employee() {name = "Alice", department_id = d1.id});
        var e2 = db.EmployeeTable.Add(new Employee() {name = "Bob", department_id = d2.id});
        var e3 = db.EmployeeTable.Add(new Employee() {name = "Charlie", department_id = d1.id});
        var de1 = db.DeskTable.Add(new Desk() {name = "Desk X"});
        var de2 = db.DeskTable.Add(new Desk() {name = "Desk Y"});
        de1.AddToEmployees(e1);
        de1.AddToEmployees(e2);
        de2.AddToEmployees(e1);
        de2.AddToEmployees(e3);

    }

    [Test]
    public void TestToArray()
    {
        var table = (ITable<Employee>)db.EmployeeTable;
        
        var employeeArray = table.ToArray();
        Assert.That(employeeArray, Is.Not.Null);
        Assert.That(employeeArray.Length, Is.EqualTo(3));
    }

    [Test]
    public void TestAllTables()
    {
        var db = (IDatabase) this.db;
        for (var i = 0; i < db.Tables.Length; i++)
        {
            var table = db.Tables[i];
            Assert.That(table, Is.Not.Null);
        }
    }
}