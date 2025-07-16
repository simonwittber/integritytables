namespace IntegrityTables.Tests;

[TestFixture]
public class UnitTests_Adaptor
{
    
   private DepartmentMetadata _adapter;

    [SetUp]
    public void Setup()
    {
        _adapter = new DepartmentMetadata();
    }

    [Test]
    public void Names_ReturnsCorrectFieldNames()
    {
        var expectedNames = new[] { "id", "location_id", "name" };
        Assert.That(_adapter.Names, Is.EqualTo(expectedNames));
    }

    [Test]
    public void Types_ReturnCorrectFieldTypes()
    {
        var expectedTypes = new[] { typeof(int), typeof(int), typeof(string) };
        Assert.That(_adapter.Types, Is.EqualTo(expectedTypes));
    }

    [Test]
    public void ReferencedTypes_ReturnCorrectReferencedTypes()
    {
        var expectedReferencedTypes = new[] { null, typeof(Location), null };
        Assert.That(_adapter.ReferencedTypes, Is.EqualTo(expectedReferencedTypes));
    }
    
    [Test]
    public void Foreach_IteratesOverAllFields()
    {
        var row = new Row<Department>
        {
            id = 1,
            data = new Department { location_id = 10, name = "HR" }
        };

        var expectedValues = new object[] { 1, 10, "HR" };
        var actualValues = new List<object>();

        foreach (var value in _adapter)
        {
            actualValues.Add(_adapter.Get(row, value.index));
        }

        Assert.That(actualValues, Is.EqualTo(expectedValues));
    }

    [Test]
    public void Get_ReturnsCorrectValuesByIndex()
    {
        var row = new Row<Department>
        {
            id = 1,
            data = new Department { location_id = 10, name = "HR" }
        };

        Assert.That(_adapter.Get(row, 0), Is.EqualTo(1));
        Assert.That(_adapter.Get(row, 1), Is.EqualTo(10));
        Assert.That(_adapter.Get(row, 2), Is.EqualTo("HR"));
    }

    [Test]
    public void Set_UpdatesValuesByIndex()
    {
        var row = new Row<Department>
        {
            id = 1,
            data = new Department { location_id = 10, name = "HR" }
        };

        _adapter.Set(ref row, 1, 20); // Update location_id
        _adapter.Set(ref row, 2, "Engineering"); // Update name

        Assert.That(row.data.location_id, Is.EqualTo(20));
        Assert.That(row.data.name, Is.EqualTo("Engineering"));
    }

    [Test]
    public void IndexOf_ReturnsCorrectIndexForFieldName()
    {
        Assert.That(_adapter.IndexOf("id"), Is.EqualTo(0));
        Assert.That(_adapter.IndexOf("location_id"), Is.EqualTo(1));
        Assert.That(_adapter.IndexOf("name"), Is.EqualTo(2));
        Assert.That(_adapter.IndexOf("nonexistent"), Is.EqualTo(-1));
    }

    [Test]
    public void GetInfo_ReturnsCorrectMetadataByIndex()
    {
        var info = _adapter.GetInfo(1);
        Assert.That(info.index, Is.EqualTo(1));
        Assert.That(info.name, Is.EqualTo("location_id"));
        Assert.That(info.type, Is.EqualTo(typeof(int)));
        Assert.That(info.referencedType, Is.EqualTo(typeof(Location)));
    }

    [Test]
    public void GetInfo_ThrowsForInvalidIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _adapter.GetInfo(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _adapter.GetInfo(3));
    }

    
}


