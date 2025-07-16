namespace IntegrityTables.Tests;

[TestFixture]
public class TriggerTests
{
    private Table<Employee> table;
    private bool triggerInvoked;

    [SetUp]
    public void Setup()
    {
        table = new Table<Employee>();
        triggerInvoked = false;

    }

    [Test]
    public void TestOnRowModifiedAdd()
    {
        int index = -1;
        TableOperation op = TableOperation.None;

        table.OnRowModified += (int i, TableOperation operation) =>
        {
            index = i;
            op = operation;
        };
        var row = table.Add(new Employee {name = "John"});
        
        // Now it should be notified
        Assert.That(index, Is.EqualTo(0));
        Assert.That(op, Is.EqualTo(TableOperation.Add));
    }

    [Test]
    public void TestOnRowModifiedRemove()
    {
        int index = -1;
        TableOperation op = TableOperation.None;
        var row = table.Add(new Employee {name = "John"});

        table.OnRowModified += (int i, TableOperation operation) =>
        {
            index = i;
            op = operation;
        };
        table.Remove(row);
        // Now it should be notified
        Assert.That(index, Is.EqualTo(0));
        Assert.That(op, Is.EqualTo(TableOperation.Remove));
    }

    [Test]
    public void TestOnRowModifiedUpdate()
    {
        int index = -1;
        TableOperation op = TableOperation.None;
        var row = table.Add(new Employee {name = "John"});

        table.OnRowModified += (int i, TableOperation operation) =>
        {
            index = i;
            op = operation;
        };
        row.name("Boris");
        table.Update(ref row);
        // Now it should be notified
        Assert.That(index, Is.EqualTo(0));
        Assert.That(op, Is.EqualTo(TableOperation.Update));
    }

    [Test]
    public void BeforeAdd_TriggerIsInvoked()
    {
        table.BeforeAdd += (ref Row<Employee> row) => triggerInvoked = true;

        table.Add(new Employee {name = "Alice"});

        Assert.That(triggerInvoked, Is.True);
    }

    [Test]
    public void AfterAdd_TriggerIsInvoked()
    {
        table.AfterAdd += (in Row<Employee> row) => triggerInvoked = true;

        table.Add(new Employee {name = "Bob"});

        Assert.That(triggerInvoked, Is.True);
    }

    [Test]
    public void BeforeUpdate_TriggerIsInvoked()
    {
        var row = table.Add(new Employee {name = "Charlie"});
        table.BeforeUpdate += (in Row<Employee> oldRow, ref Row<Employee> row) => triggerInvoked = true;

        row.data.name = "Updated Charlie";
        table.Update(ref row);

        Assert.That(triggerInvoked, Is.True);
    }

    [Test]
    public void AfterUpdate_TriggerIsInvoked()
    {
        var row = table.Add(new Employee {name = "Diana"});
        table.AfterUpdate += (in Row<Employee> oldRow, in Row<Employee> row) => triggerInvoked = true;

        row.data.name = "Updated Diana";
        table.Update(ref row);

        Assert.That(triggerInvoked, Is.True);
    }

    [Test]
    public void BeforeRemove_TriggerIsInvoked()
    {
        var row = table.Add(new Employee {name = "Eve"});
        table.BeforeRemove += (in Row<Employee> row) => triggerInvoked = true;

        table.Remove(in row);

        Assert.That(triggerInvoked, Is.True);
    }

    [Test]
    public void AfterRemove_TriggerIsInvoked()
    {
        var row = table.Add(new Employee {name = "Frank"});
        table.AfterRemove += (in Row<Employee> row) => triggerInvoked = true;

        table.Remove(in row);

        Assert.That(triggerInvoked, Is.True);
    }
}