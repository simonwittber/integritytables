namespace IntegrityTables.Tests;


[TestFixture]
public class TestRowAdapter
{
    private HumanResourcesDatabase db;
    
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }
        
    [Test]
    public void TestRowAdapterSimple()
    {
        var itable = db.EmployeeTable as ITable;
        
        var adapter = itable.CreateEmptyRow();
        adapter["name"] = "John";
        itable.Add(adapter);
        
        // First row in the table is the one we just added
        // id should match.
        Assert.That(db.EmployeeTable[0].id, Is.EqualTo(adapter.id));

        // check positional index operator
        Assert.That(adapter[1], Is.EqualTo("John"));
        
        // check named index operator
        Assert.That(adapter["name"], Is.EqualTo("John"));
        
        adapter["name"] = "Bob";
        itable.Update(adapter);
        
        // Check that the name was updated
        Assert.That(db.EmployeeTable[0].data.name, Is.EqualTo("Bob"));
    }
    
}