namespace IntegrityTables.Tests;



[TestFixture]
public class ChangeSetBehaviorTests
{
    public struct DummyData : IEquatable<DummyData>
    {
        public int Value;

        public bool Equals(DummyData other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is DummyData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(DummyData left, DummyData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DummyData left, DummyData right)
        {
            return !left.Equals(right);
        }
    }

    private HumanResourcesDatabase db;
    
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestNestedChangeSet()
    {
        using (var cs1 = db.NewChangeSet())
        {
            db.LocationTable.Add(new Location() { name = "Location1" });
            using(var cs2 = db.NewChangeSet())
            {
                db.LocationTable.Add(new Location() { name = "Location2" });
                cs2.Commit();
            }
            db.LocationTable.Add(new Location() { name = "Location3" });
            cs1.Commit();
        }
        // Check that all locations were added
        var locations = db.LocationTable.ToList();
        Assert.That(locations.Count, Is.EqualTo(3), "All locations should be added.");
        Assert.That(locations[0].name(), Is.EqualTo("Location1"), "First location should be Location1.");
        Assert.That(locations[1].name(), Is.EqualTo("Location2"), "Second location should be Location2.");
        Assert.That(locations[2].name(), Is.EqualTo("Location3"), "Third location should be Location3.");
    }

    [Test]
    public void TestWarningOnNoCommit()
    {
        Warnings.ClearEventCallbacks();
        var warningMsg = string.Empty;
        Warnings.OnWarning += (msg) =>
        {
            warningMsg = msg;
        };
        using (db.NewChangeSet())
        {
            db.DepartmentTable.Add(new Department {name = "HR"});
            db.EmployeeTable.Add(new Employee {name = "John Doe"});
        }
        Assert.That(warningMsg, Is.Not.Null);
        Assert.That(warningMsg, Is.Not.Empty);
        Assert.That(warningMsg, Does.Contain("ChangeSet not committed"));
    }
    
    [Test]
    public void TestNoWarningOnCommit()
    {
        Warnings.ClearEventCallbacks();
        var warningMsg = string.Empty;
        Warnings.OnWarning += (msg) =>
        {
            warningMsg = msg;
        };
        using (var changeSet = db.NewChangeSet())
        {
            db.DepartmentTable.Add(new Department {name = "HR"});
            db.EmployeeTable.Add(new Employee {name = "John Doe"});
            changeSet.Commit();
        }
        Assert.That(warningMsg, Is.Empty);
    }
    
    [Test]
    public void TestNoWarningOnRollback()
    {
        Warnings.ClearEventCallbacks();
        var warningMsg = string.Empty;
        Warnings.OnWarning += (msg) =>
        {
            warningMsg = msg;
        };
        using (var changeSet = db.NewChangeSet())
        {
            db.DepartmentTable.Add(new Department {name = "HR"});
            db.EmployeeTable.Add(new Employee {name = "John Doe"});
            changeSet.Rollback();
        }
        Assert.That(warningMsg, Is.Empty);
    }
    
    [Test]
    public void CommitChangeSet_PersistsAllAdds()
    {
        var table = new Table<DummyData>();
        Assert.That(table.Count, Is.EqualTo(0));

        using (var cs = new ChangeSet(table))
        {
            table.Add(new DummyData {Value = 1});
            table.Add(new DummyData {Value = 2});
            Assert.That(table.Count, Is.EqualTo(2), "Within ChangeSet, adds should be visible.");

            cs.Commit();
        }

        // After commit, rows remain
        Assert.That(table.Count, Is.EqualTo(2), "After Commit(), rows should persist.");
    }

    [Test]
    public void DisposeWithoutCommit_RollsBackAllAdds()
    {
        var table = new Table<DummyData>();
        Assert.That(table.Count, Is.EqualTo(0));

        using (new ChangeSet(table))
        {
            table.Add(new DummyData {Value = 42});
            Assert.That(table.Count, Is.EqualTo(1), "Within ChangeSet, add should be visible.");
            // no cs.Commit()
        } // Dispose() called here → Rollback

        Assert.That(table.Count, Is.EqualTo(0), "Without Commit(), disposing ChangeSet should roll back.");
    }

    [Test]
    public void ExceptionInsideUsing_RollsBackOnDispose()
    {
        var table = new Table<DummyData>();
        Assert.That(table.Count, Is.EqualTo(0));

        try
        {
            using (new ChangeSet(table))
            {
                table.Add(new DummyData {Value = 7});
                Assert.That(table.Count, Is.EqualTo(1), "Before exception, add should be visible.");
                throw new InvalidOperationException("boom");
            }
        }
        catch (InvalidOperationException)
        {
        }

        // even though we never reached Commit(), Dispose should have rolled back
        Assert.That(table.Count, Is.EqualTo(0), "Exception in ChangeSet scope should roll back on Dispose().");
    }

    [Test]
    public void OperationsOutsideChangeSet_AreIndependent()
    {
        var table = new Table<DummyData>();
        table.AddConstraint((in Row<DummyData> d) => d.data.Value >= 0, "Value >= 0");

        // Add a negative value → fails
        var bad = new DummyData {Value = -1};
        Assert.Throws<InvalidOperationException>(() => table.Add(bad), "Negative value should violate constraint");

        // Table should still be empty and accept new valid adds
        Assert.That(table.Count, Is.EqualTo(0), "Failed add should not leave partial state.");
        table.Add(new DummyData {Value = 5});
        Assert.That(table.Count, Is.EqualTo(1), "Valid add outside ChangeSet should succeed after a failure.");
    }
    
    [Test]
    public void RemoveWithExistingReference_ThenAdd_ShouldThrowTransactionFailed()
    {
        // Arrange: add a department and an employee in it
        Row<Department> d;
        using (new ChangeSet())
        {
            d = db.DepartmentTable.Add(new Department {name = "HR"});
            db.DepartmentTable.Add(new Department {name = "HR2"});
            db.DepartmentTable.Add(new Department {name = "HR3"});
            db.EmployeeTable.Add(new Employee {department_id = d.id});
        }
        // Act & Assert #1: removing the department fails (because the employee still points at it)
        var ex = Assert.Throws<InvalidOperationException>(() => db.DepartmentTable.Remove(d));
        StringAssert.Contains("contains reference", ex.Message);
        
        db.DepartmentTable.Add(new Department {name = "HRX"});

        Assert.DoesNotThrow(() => 
            db.EmployeeTable.Add(new Employee { department_id = 0 }));
    }

   
}