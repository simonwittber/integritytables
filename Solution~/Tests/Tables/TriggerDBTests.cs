namespace IntegrityTables.Tests;

[TestFixture]
public class TriggerDBTests
{
    TriggerDB triggerDB;
    [SetUp]
    public void Setup()
    {
        triggerDB = new TriggerDB();
    }
    
    [Test]
    public void TestBeforeAdd()
    {
        Assert.That(triggerDB.TriggerTableBTable.Count, Is.EqualTo(0));
        var x = triggerDB.TriggerTableATable.Add(new TriggerTableA());
        Assert.That(triggerDB.TriggerTableBTable.Count, Is.EqualTo(2));

        foreach(var id in triggerDB.TriggerTableBTable)
        {
            var row = triggerDB.TriggerTableBTable.Get(id);
            if (row.data.b_id == 0)
            {
                Assert.That(row.data.beforeAdd, Is.True);
                Assert.That(row.data.b_id, Is.Zero);
            }

            if (row.data.b_id == 1)
            {
                Assert.That(row.data.afterAdd, Is.True);
                Assert.That(row.data.b_id, Is.EqualTo(x.id));
            }

        }

        x.data.text = "Simon";
        // Assert.That(triggerDB.TriggerTableATable.BeforeUpdate, Is.Not.Null);
        triggerDB.TriggerTableATable.Update(ref x);
        Assert.That(triggerDB.TriggerTableBTable.Count, Is.EqualTo(4));
        foreach(var id in triggerDB.TriggerTableBTable)
        {
            var row = triggerDB.TriggerTableBTable.Get(id);
            if (row.data.b_id == 2)
            {
                Assert.That(row.data.beforeUpdate, Is.True);
                Assert.That(row.data.b_id, Is.EqualTo(x.id));
            }

            if (row.data.b_id == 3)
            {
                Assert.That(row.data.afterUpdate, Is.True);
                Assert.That(row.data.b_id, Is.EqualTo(x.id));
            }
        }
        
        triggerDB.TriggerTableATable.Remove(in x);
        Assert.That(triggerDB.TriggerTableBTable.Count, Is.EqualTo(6));
        foreach(var id in triggerDB.TriggerTableBTable)
        {
            var row = triggerDB.TriggerTableBTable.Get(id);
            if (row.data.b_id == 4)
            {
                Assert.That(row.data.beforeRemove, Is.True);
                Assert.That(row.data.b_id, Is.EqualTo(x.id));
            }

            if (row.data.b_id == 5)
            {
                Assert.That(row.data.afterRemove, Is.True);
                Assert.That(row.data.b_id, Is.EqualTo(x.id));
            }
        }
        
    }
}