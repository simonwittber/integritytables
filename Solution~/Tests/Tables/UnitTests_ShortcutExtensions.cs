namespace IntegrityTables.Tests;

public class Tests_ShortcutExtensions
{
    private HumanResourcesDatabase _db;

    [SetUp]
    public void Setup()
    {
        _db = new HumanResourcesDatabase();
    }

    [Test]
    public void GetTest()
    {
        var exists = _db.EmployeeTable.TryGet(0, out Row<Employee> _);
        Assert.That(exists, Is.False);
    }

    [Test]
    public void AddTest()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        Assert.That(e.data.name, Is.EqualTo("Simon Says"));
        Assert.That(e.id, Is.Not.EqualTo(0));
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void UpdateTest()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        e.data.name = "Simon Says X";
        _db.EmployeeTable.Update(ref e);
        e = _db.EmployeeTable.Get(e.id);
        Assert.That(e.data.name, Is.EqualTo("Simon Says X"));
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveTest()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        _db.EmployeeTable.Remove(in e);
        var exists = _db.EmployeeTable.TryGet(e.id, out e);
        Assert.That(exists, Is.False);
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveThenUpdateTest()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        _db.EmployeeTable.Remove(in e);
        Assert.Throws<KeyNotFoundException>(() => { _db.EmployeeTable.Update(ref e); });
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(0));
    }
}