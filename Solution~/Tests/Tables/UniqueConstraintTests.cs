namespace IntegrityTables.Tests;

[TestFixture]
public class UniqueConstraintTests
{
    private Table<Department> _table;

    [SetUp]
    public void Setup()
    {
        _table = new Table<Department>();

        ValueTuple<string> GetKeyFunc(in Row<Department> row) => new ValueTuple<string>(row.data.name);

        _table.AddUniqueIndex("UniqueName", GetKeyFunc); // Add a unique constraint on the `name` field
    }

    [Test]
    public void Add_ThrowsWhenUniqueConstraintIsViolated()
    {
        var row1 = new Row<Department> {data = new Department {name = "HR"}};
        _table.Add(ref row1);

        var row2 = new Row<Department> {data = new Department {name = "HR"}};
        Assert.Throws<InvalidOperationException>(() => _table.Add(ref row2));

        Assert.That(_table.Count, Is.EqualTo(1));
    }

    [Test]
    public void Update_ThrowsWhenUniqueConstraintIsViolated()
    {
        var row1 = new Row<Department> {data = new Department {name = "HR"}};
        var row2 = new Row<Department> {data = new Department {name = "Engineering"}};
        _table.Add(ref row1);
        _table.Add(ref row2);

        row2.data.name = "HR";
        Assert.Throws<InvalidOperationException>(() => _table.Update(ref row2));

        // Assert: Verify that the rows remain unchanged
        var storedRow1 = _table.Get(row1.id);
        var storedRow2 = _table.Get(row2.id);
        Assert.That(storedRow1.data.name, Is.EqualTo("HR"));
        Assert.That(storedRow2.data.name, Is.EqualTo("Engineering"));
    }
    
    [Test]
    public void Update_ThrowsAndRollbackRestoresState()
    {
        _table.BeginChangeSet();
        var row1 = new Row<Department> {data = new Department {name = "HR"}};
        var row2 = new Row<Department> {data = new Department {name = "Engineering"}};
        _table.Add(ref row1);
        _table.Add(ref row2);
        _table.CommitChangeSet();
        _table.BeginChangeSet();
        row2.data.name = "HR";
        Assert.Throws<InvalidOperationException>(() => _table.Update(ref row2));
        _table.RollbackChangeSet();
        var storedRow1 = _table.Get(row1.id);
        var storedRow2 = _table.Get(row2.id);
        Assert.That(storedRow1.data.name, Is.EqualTo("HR"));
        Assert.That(storedRow2.data.name, Is.EqualTo("Engineering"));
    }

    [Test]
    public void Test_GetByUniqueIndex()
    {
        var row1 = _table.Add(new Department {name = "HR"});
        var row2 = _table.Add(new Department {name = "Engineering"});
        var row3 = _table.Add(new Department {name = "IT"});

        Assert.That(_table.GetByUniqueIndex("UniqueName", "HR").id, Is.EqualTo(row1.id));
        Assert.That(_table.GetByUniqueIndex("UniqueName", "IT").id, Is.EqualTo(row3.id));
        Assert.That(_table.GetByUniqueIndex("UniqueName", "Engineering").id, Is.EqualTo(row2.id));

    }
}
