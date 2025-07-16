namespace IntegrityTables.Tests;

public class Tests_Internal
{
    private HumanResourcesDatabase _humanResourcesDatabase;

    [SetUp]
    public void Setup()
    {
        _humanResourcesDatabase = new HumanResourcesDatabase();
    }

    [Test]
    public void TestRowIndex()
    {
        var e0 = _humanResourcesDatabase.EmployeeTable.Add(new Employee());
        Assert.That(e0._index, Is.EqualTo(0));
        var e1 = _humanResourcesDatabase.EmployeeTable.Add(new Employee());
        Assert.That(e1._index, Is.EqualTo(1));
        var e2 = _humanResourcesDatabase.EmployeeTable.Add(new Employee());
        Assert.That(e2._index, Is.EqualTo(2));
        _humanResourcesDatabase.EmployeeTable.Remove(in e0);
    }
}