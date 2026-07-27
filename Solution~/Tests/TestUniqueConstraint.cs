namespace Tests;

[TestFixture]
public class TestUniqueConstraint
{
    private Database db;

    [SetUp]
    public void Setup()
    {
        db = new Database();
    }

    [Test]
    public void TestAdd()
    {
        Assert.DoesNotThrow(() =>
            db.LocationTable.Add(name: "New York")
        );
    }

    [Test]
    public void TestTryGetBy()
    {
        db.LocationTable.Add(name: "New York");
        db.LocationTable.Add(name: "Sydney");
        Assert.That(db.LocationTable.TryGetByName("New York", out var row), Is.True);
        Assert.That(row.name, Is.EqualTo("New York"));
        Assert.That(db.LocationTable.TryGetByName("Sydney", out row), Is.True);
        Assert.That(row.name, Is.EqualTo("Sydney"));
        Assert.That(db.LocationTable.TryGetByName("Los Angeles", out row), Is.False);
        Assert.That(row, Is.EqualTo(default(LocationRow)));
    }

    [Test]
    public void TestAddTwice()
    {
        db.LocationTable.Add(name: "New York");
        Assert.Throws<InvalidOperationException>(() =>
            db.LocationTable.Add(name: "New York")
        );
    }

    [Test]
    public void TestAddRemoveAdd()
    {
        db.LocationTable.Add(name: "New York");
        db.LocationTable.Remove(0);
        Assert.DoesNotThrow(() =>
            db.LocationTable.Add(name: "New York")
        );
    }
}