namespace IntegrityTables.Tests;

public class TestsChangeSet
{
    private HumanResourcesDatabase db;

    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void AddTest()
    {
        Row<Employee> e;
        using (var cs = db.NewChangeSet()) {
            e = db.EmployeeTable.Add(new Employee {name = "Simon Says"});
            cs.Commit();
        }
        Assert.That(e.data.name, Is.EqualTo("Simon Says"));
        Assert.That(e.id, Is.Not.EqualTo(0));
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void UpdateTest()
    {
        db.EmployeeTable.BeginChangeSet();
        var e = db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        db.EmployeeTable.CommitChangeSet();
        using (var tx = new ChangeSet(db.EmployeeTable))
        {
            e.data.name = "Simon Says X";
            db.EmployeeTable.Update(ref e);
            e = db.EmployeeTable.Get(e.id);
            Assert.That(e.data.name, Is.EqualTo("Simon Says X"));
            tx.Rollback();
        }

        e = db.EmployeeTable.Get(e.id);
        Assert.That(e.data.name, Is.EqualTo("Simon Says"));
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveTest()
    {
        Row<Employee> e;
        using (var tx = new ChangeSet(db.EmployeeTable))
        {
            e = db.EmployeeTable.Add(new Employee {name = "Simon Says"});
            tx.Commit();
        }

        using (var tx = new ChangeSet(db.EmployeeTable))
        {
            db.EmployeeTable.Remove(in e);
            Assert.That(db.EmployeeTable.Count, Is.EqualTo(0));
            Assert.That(db.EmployeeTable.ContainsKey(e.id), Is.False);
            tx.Rollback();
        }

        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
        Assert.That(db.EmployeeTable.ContainsKey(e.id), Is.True);

        Assert.DoesNotThrow(() => { e = db.EmployeeTable.Get(e.id); });
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void RemoveThenUpdateTest()
    {
        db.EmployeeTable.BeginChangeSet();
        var e = db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        db.EmployeeTable.Remove(in e);
        db.EmployeeTable.CommitChangeSet();

        Assert.Throws<KeyNotFoundException>(() => { db.EmployeeTable.Update(ref e); });
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(0));
        
    }
    
}