namespace IntegrityTables.Tests;

[TestFixture]
public class UnitTests_Bugs
{
    [Test]
    public void TableLoad_ResetsRowCount()
    {
        var table = new Table<Employee>();
        table.Add(new Employee {name = "A"});
        table.Add(new Employee {name = "B"});
        Assert.That(table.Count, Is.EqualTo(2), "Precondition: two rows should be present");

        // load empty set => expected count 0, but Count remains stale
        table.Load(Array.Empty<Row<Employee>>());
        Assert.That(table.Count, Is.EqualTo(0), "Load should clear existing rows");
    }

    [Test]
    public void QueryEnumerator_FiltersByNameWithoutException()
    {
        var db = new Table<Employee>();
        db.Add(new Employee {name = "Alice"});
        db.Add(new Employee {name = "Bob"});
        db.Add(new Employee {name = "Charlie"});

        Assert.DoesNotThrow(() =>
        {
            var results = db.Query((in Row<Employee> row) => row.data.name.StartsWith("A")).ToArray();
            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].data.name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void RemoveRollback_RestoresOriginalVersion()
    {
        var table = new Table<Employee>();
        table.BeginChangeSet();
        var initial = table.Add(new Employee {name = "X"});
        // bump version to 1
        initial.data.name = "Y";
        table.Update(ref initial);
        Assert.That(table.Get(initial.id)._version, Is.EqualTo(1), "Precondition: version should be incremented");
        table.CommitChangeSet();
        // perform removal inside a transaction and then rollback
        using (var tx = new ChangeSet(table))
        {
            table.Remove(in initial);
            tx.Rollback();
        }

        var after = table.Get(initial.id);
        Assert.That(after._version, Is.EqualTo(1), "Rollback of Remove should restore the original version");
    }

    [Test]
    public void FailedUpdate_DoesNotCorruptUniqueIndex()
    {
        var table = new Table<Employee>();

        ValueTuple<string> GetNameFunc(in Row<Employee> r) => new ValueTuple<string>(r.data.name);

        table.AddUniqueIndex<ValueTuple<string>>("name", GetNameFunc);
        table.Add(new Employee {name = "A"});
        var row2 = table.Add(new Employee {name = "B"});

        // attempt invalid update: change B -> A (duplicate)
        row2.data.name = "A";
        Assert.Throws<InvalidOperationException>(() => table.Update(ref row2));

        // table should still enforce uniqueness on "B"
        Assert.Throws<InvalidOperationException>(() => table.Add(new Employee {name = "B"}));
    }

    [Test]
    public void AddUniqueIndex_ThrowsForExistingDuplicates()
    {
        var table = new Table<Employee>();
        table.Add(new Employee {name = "A"});
        table.Add(new Employee {name = "A"}); // duplicate

        ValueTuple<string> GetNameFunc(in Row<Employee> r) => new ValueTuple<string>(r.data.name);

        // creating a unique index on 'name' should detect the existing duplicates
        Assert.Throws<InvalidOperationException>(() =>
            table.AddUniqueIndex<ValueTuple<string>>("name", GetNameFunc)
        );
    }

    [Test]
    public void Transaction_DisposeRollsBackOnException()
    {
        var table = new Table<Employee>();
        try
        {
            using (new ChangeSet(table))
            {
                table.Add(new Employee {name = "X"});
                throw new InvalidOperationException("Simulated failure");
            }
        }
        catch (InvalidOperationException)
        {
            /* ignore */
        }

        // because Dispose currently calls Commit(), the row remains; expected rollback => count==0
        Assert.That(table.Count, Is.EqualTo(0), "Transaction on exception should roll back, not commit");
    }

    [Test]
    public void MultiDelete_Restore()
    {
        var db = new HumanResourcesDatabase();
        Row<Employee> e2;
        Row<Employee> e3;
        Row<Employee> e4;
        using (var cs = db.NewChangeSet())
        {
            db.DepartmentTable.Add(new Department() {name = "HR"});
            db.EmployeeTable.Add(new Employee {name = "A"});
            e2 = db.EmployeeTable.Add(new Employee {name = "B"});
            e3 = db.EmployeeTable.Add(new Employee {name = "C"});
            e4 = db.EmployeeTable.Add(new Employee {name = "D"});
            cs.Commit();
        }

        var data = db.EmployeeTable.ToArray();

        Assert.That(data.Length, Is.EqualTo(4), "Precondition: 4 rows should be present");
        Assert.That(data[0].id, Is.EqualTo(1));
        Assert.That(data[0].data.name, Is.EqualTo("A"));
        Assert.That(data[1].id, Is.EqualTo(2));
        Assert.That(data[1].data.name, Is.EqualTo("B"));
        Assert.That(data[2].id, Is.EqualTo(3));
        Assert.That(data[2].data.name, Is.EqualTo("C"));
        Assert.That(data[3].id, Is.EqualTo(4));
        Assert.That(data[3].data.name, Is.EqualTo("D"));


        using (var cs = db.NewChangeSet())
        {
            db.EmployeeTable.Remove(e2);
            db.EmployeeTable.Remove(e3);
            db.EmployeeTable.Remove(e4);
            cs.Rollback();
        }

    }
}