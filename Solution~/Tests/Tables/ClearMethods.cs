namespace IntegrityTables.Tests;

[TestFixture]
public class ClearMethods
{
    private HumanResourcesDatabase db;
    
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void ClearResetsKeyGenerator()
    {
        db.EmployeeTable.Add(new Employee());
        db.EmployeeTable.Add(new Employee());
        db.EmployeeTable.Clear();
        var row = db.EmployeeTable.Add(new Employee());
        Assert.That(row.id, Is.EqualTo(1));
    }
    
    [Test]
    public void ClearResetsKeyGeneratorAfterChangeSetCommit()
    {
        db.EmployeeTable.Add(new Employee());
        db.EmployeeTable.Add(new Employee());
        using(var changeSet = db.NewChangeSet())
        {
            db.EmployeeTable.Add(new Employee());
            db.EmployeeTable.Add(new Employee());
            db.EmployeeTable.Clear();
            changeSet.Commit();
        }
        var row = db.EmployeeTable.Add(new Employee());
        Assert.That(row.id, Is.EqualTo(1));
    }
    
    [Test]
    public void ClearDoesNotResetKeyGeneratorAfterChangeSetRollback()
    {
        
        db.EmployeeTable.Add(new Employee());
        db.EmployeeTable.Add(new Employee());
        using(var changeSet = db.NewChangeSet())
        {
            db.EmployeeTable.Add(new Employee());
            db.EmployeeTable.Add(new Employee());
            db.EmployeeTable.Clear();
            changeSet.Rollback();
        }
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(2));
        // new row should have id 5, as the last id was 4 even though it was rolled back
        var row = db.EmployeeTable.Add(new Employee());
        Assert.That(row.id, Is.EqualTo(5));
    }
}