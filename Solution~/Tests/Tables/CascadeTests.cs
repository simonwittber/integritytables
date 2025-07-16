namespace IntegrityTables.Tests;

[TestFixture]
public class CascadeTests
{
    private HumanResourcesDatabase db;
    [SetUp]
    public void SetUp()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestCascadeDelete()
    {
        var a = db.ParentTable.Add(new Parent());
        var b = db.ChildTable.Add(new Child { parentId = a.id });
        db.ChildTable.Add(new Child { parentId = b.id });
        
        Assert.Throws<InvalidOperationException>(() => db.ParentTable.Remove(a), "Should not be able to remove CascadeTableA with children in CascadeTableB");
        
        
        db.ParentTable.Remove(a, CascadeOperation.Delete);
        
        Assert.That(db.ParentTable.Count, Is.EqualTo(0));
        Assert.That(db.ChildTable.Count, Is.EqualTo(0));
        Assert.That(db.CascadeTableCTable.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TestCascadeDeleteClause()
    {
        var a = db.ParentTable.Add(new Parent());
        var b = db.ChildTable.Add(new Child { parentId = a.id });
        db.ChildTable.Add(new Child { parentId = b.id });
        
        Assert.Throws<InvalidOperationException>(() => db.ParentTable.Remove(a), "Should not be able to remove CascadeTableA with children in CascadeTableB");
        
        db.ParentTable.Remove((in Row<Parent> row) => true, CascadeOperation.Delete);
        
        Assert.That(db.ParentTable.Count, Is.EqualTo(0));
        Assert.That(db.ChildTable.Count, Is.EqualTo(0));
        Assert.That(db.CascadeTableCTable.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TestClearCascade()
    {
        var a = db.ParentTable.Add(new Parent());
        var b = db.ChildTable.Add(new Child { parentId = a.id });
        db.ChildTable.Add(new Child { parentId = b.id });
        
        Assert.Throws<InvalidOperationException>(() => db.ParentTable.Remove(a), "Should not be able to remove CascadeTableA with children in CascadeTableB");

        db.ParentTable.Clear(CascadeOperation.Delete);
        
        Assert.That(db.ParentTable.Count, Is.EqualTo(0));
        Assert.That(db.ChildTable.Count, Is.EqualTo(0));
        Assert.That(db.CascadeTableCTable.Count, Is.EqualTo(0));
    }
    
    [Test]
    public void TestClearCascadeObservers()
    {
        var a = db.ParentTable.Add(new Parent());
        var b = db.ChildTable.Add(new Child { parentId = a.id });
        var child = db.ChildTable.Add(new Child { parentId = b.id });

        
        var modified = false;
        db.ChildTable.AddObserver(child, row =>
        {
            modified = true;
        });
        
        db.ParentTable.Clear(CascadeOperation.SetNull);
        Assert.That(modified, Is.True);        
    }
}