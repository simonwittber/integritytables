using System.Collections;
using System.Text.Json;

namespace IntegrityTables.Tests;


[TestFixture]
public class TestPersistence
{


    [SetUp]
    public void Setup()
    {
        
    }

    private static void CreateTestData(HumanResourcesDatabase db)
    {
        using var scope = db.CreateContext();
        var d1 = db.DepartmentTable.Add(new Department() { name = "Engineering" });
        var d2 = db.DepartmentTable.Add(new Department() { name = "Marketing" });
        var e1= db.EmployeeTable.Add(new Employee() { name = "Alice", department_id = d1.id });
        var e2 = db.EmployeeTable.Add(new Employee() { name = "Bob", department_id = d2.id });
        var e3 = db.EmployeeTable.Add(new Employee() { name = "Charlie", department_id = d1.id });
        var de1 = db.DeskTable.Add(new Desk() { name = "Desk X" });
        var de2 = db.DeskTable.Add(new Desk() { name = "Desk Y" });
        de1.AddToEmployees(e1);
        de1.AddToEmployees(e2);
        de2.AddToEmployees(e1);
        de2.AddToEmployees(e3);
    }

    [Test]
    public void TestDatabasePersistenceAPI()
    {
        Warnings.OnLog += s => System.Console.WriteLine($"{s}");
        Warnings.OnWarning += s => System.Console.WriteLine($"Warning: {s}");

        var db = new HumanResourcesDatabase();
        var path = Path.Combine(Path.GetTempPath(), "IntegrityTablesTests");
        db.Persistence = new DatabaseJsonSerializer(path);
        CreateTestData(db);
        db.Save();
        db.Load();
        CheckTestData(db);
        
        
        var newDb = new HumanResourcesDatabase();
        newDb.Persistence = new DatabaseJsonSerializer(path);
        newDb.Load();
        CheckTestData(newDb);
    }

    private void CheckTestData(HumanResourcesDatabase db)
    {
        Assert.IsTrue(db.DepartmentTable.Exists(d => d.name == "Engineering"), "Engineering department should exist");
        Assert.IsTrue(db.DepartmentTable.Exists(d => d.name == "Marketing"), "Marketing department should exist");
        Assert.IsTrue(db.EmployeeTable.Exists(e => e.name == "Alice"), "Alice should exist");
        Assert.IsTrue(db.EmployeeTable.Exists(e => e.name == "Bob"), "Bob should exist");
        Assert.IsTrue(db.EmployeeTable.Exists(e => e.name == "Charlie"), "Charlie should exist");
        Assert.IsTrue(db.DeskTable.Exists(d => d.name == "Desk X"), "Desk X should exist");
        Assert.IsTrue(db.DeskTable.Exists(d => d.name == "Desk Y"), "Desk Y should exist");
    }
}