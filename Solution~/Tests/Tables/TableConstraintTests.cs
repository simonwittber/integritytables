namespace IntegrityTables.Tests;

[TestFixture]
public class TableConstraintTests
{
    private Table<Department> _table;

    [SetUp]
    public void Setup()
    {
        _table = new Table<Department>();

        bool Func(in Row<Department> row) => row.data.name != null;

        _table.AddConstraint(Func, "Name not null"); // Add a constraint that name cannot be null
    }

    [Test]
    public void TestDeclaritiveConstraint()
    {
        var db = new HumanResourcesDatabase();
        db.ConstrainedTableTable.Add(new ConstrainedTable() { name = "Test" });
        Assert.That(db.ConstrainedTableTable.Count, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => db.ConstrainedTableTable.Add(new ConstrainedTable() { name = null }));
    }

    [Test]
    public void Add_ThrowsWhenConstraintIsViolated()
    {
        var invalidRow = new Row<Department> { data = new Department { name = null } };

        Assert.Throws<InvalidOperationException>(() => _table.Add(ref invalidRow));

        Assert.That(_table.Count, Is.EqualTo(0));
    }

    [Test]
    public void Update_ThrowsWhenConstraintIsViolated()
    {
        var validRow = new Row<Department> { data = new Department { name = "HR" } };
        _table.Add(ref validRow);

        validRow.data.name = null;
        Assert.Throws<InvalidOperationException>(() => _table.Update(ref validRow));

        var storedRow = _table.Get(validRow.id);
        Assert.That(storedRow.data.name, Is.EqualTo("HR"));
    }
}
