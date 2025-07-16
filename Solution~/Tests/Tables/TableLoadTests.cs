namespace IntegrityTables.Tests;

[TestFixture]
public class TableLoadTests
{
    private Table<Department> _table;

    [SetUp]
    public void Setup()
    {
        _table = new Table<Department>();
    }

    [Test]
    public void Load_CorrectsInvalidIndexes()
    {
        var rowsWithIncorrectIndexes = new[]
        {
            new Row<Department> { id = 1, _index = -1, data = new Department { name = "HR" } },
            new Row<Department> { id = 2, _index = 999, data = new Department { name = "Engineering" } }
        };

        _table.Load(rowsWithIncorrectIndexes);

        var loadedRows = _table.ToArray();
        Assert.That(loadedRows.Length, Is.EqualTo(2));
        Assert.That(loadedRows[0].id, Is.EqualTo(1));
        Assert.That(loadedRows[0]._index, Is.EqualTo(0)); // Index should be corrected
        Assert.That(loadedRows[1].id, Is.EqualTo(2));
        Assert.That(loadedRows[1]._index, Is.EqualTo(1)); // Index should be corrected
    }
    
    [Test]
    public void Load_CorrectsInvalidIndexesInOriginalArray()
    {
        var rowsWithIncorrectIndexes = new[]
        {
            new Row<Department> { id = 1, _index = -1, data = new Department { name = "HR" } },
            new Row<Department> { id = 2, _index = 999, data = new Department { name = "Engineering" } }
        };

        _table.Load(rowsWithIncorrectIndexes);

        Assert.That(rowsWithIncorrectIndexes.Length, Is.EqualTo(2));
        Assert.That(rowsWithIncorrectIndexes[0].id, Is.EqualTo(1));
        Assert.That(rowsWithIncorrectIndexes[0]._index, Is.EqualTo(0)); // Index should be corrected
        Assert.That(rowsWithIncorrectIndexes[1].id, Is.EqualTo(2));
        Assert.That(rowsWithIncorrectIndexes[1]._index, Is.EqualTo(1)); // Index should be corrected
    }
}
