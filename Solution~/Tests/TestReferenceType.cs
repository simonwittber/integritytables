using IntegrityTables;

namespace Tests;

[TestFixture]
public class TestReferenceType
{
    Reference<Location> field;

    
    [Test]
    public void TestReferenceDefaultsToNull()
    {
        var r = new Reference<Location>();
        Assert.That((int)r, Is.EqualTo(-1));
    }
    
    [Test]
    public void TestUninitializedFieldReferenceDefaultsToNull()
    {
        Assert.That((int)field, Is.EqualTo(-1));
    }

    [Test]
    public void TestReferenceWhenSetEqualsSet()
    {
        var r = new Reference<Location>();
        r = 0;
        Assert.That((int)r, Is.EqualTo(0));
    }
    
}