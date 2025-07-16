namespace IntegrityTables.Tests;

[TestFixture]
public class GeneratedDatabaseTests
{
    private HumanResourcesDatabase db;

    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void GetByFieldName()
    {
        var row = db.DeskTable.GetByName("Desk 1");
        Assert.That(row.id, Is.EqualTo(1));
        Assert.Throws<KeyNotFoundException>(() => db.DeskTable.GetByName("Desk XXX"));
    }
    
    [Test]
    public void TryGetByFieldName()
    {
        var test = db.DeskTable.TryGetByName("Desk 1", out var result);
        Assert.That(test, Is.True);
        Assert.That(result.id, Is.EqualTo(1));
        test = db.DeskTable.TryGetByName("Desk XXX", out result);
        Assert.That(test, Is.False);
        Assert.That(result, Is.EqualTo(default(Row<Desk>)));

    }

    [Test]
    public void Add_AddsRowSuccessfully()
    {
        // Act
        var department = db.DepartmentTable.Add(new Department { name = "HR" });

        // Assert
        Assert.That(department.id, Is.GreaterThan(0));
        Assert.That(department.data.name, Is.EqualTo("HR"));
    }

    [Test]
    public void TryAdd_AddsRowSuccessfully()
    {
        // Arrange
        var row = new Row<Department> { data = new Department { name = "Finance" } };

        // Act
        var result = db.DepartmentTable.TryAdd(ref row);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(row.id, Is.GreaterThan(0));
        Assert.That(row.data.name, Is.EqualTo("Finance"));
    }

    [Test]
    public void TryAdd_FailsForDuplicateRow()
    {
        // Arrange
        var row = db.DepartmentTable.Add(new Department { name = "IT" });
        var duplicateRow = new Row<Department> { id = row.id, data = row.data };

        // Act
        var result = db.DepartmentTable.TryAdd(ref duplicateRow);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Remove_RemovesRowSuccessfully()
    {
        // Arrange
        var row = db.DepartmentTable.Add(new Department { name = "Marketing" });

        // Act
        db.DepartmentTable.Remove(in row);

        // Assert
        Assert.That(db.DepartmentTable.TryGet(row.id, out Row<Department> _), Is.False);
    }

    [Test]
    public void Update_UpdatesRowSuccessfully()
    {
        // Arrange
        var row = db.DepartmentTable.Add(new Department { name = "Sales" });
        row.data.name = "Updated Sales";

        // Act
        db.DepartmentTable.Update(ref row);

        // Assert
        db.DepartmentTable.TryGet(row.id, out Row<Department> updatedRow);
        Assert.That(updatedRow.data.name, Is.EqualTo("Updated Sales"));
    }

    [Test]
    public void TryGet_RetrievesRowSuccessfully()
    {
        // Arrange
        var row = db.DepartmentTable.Add(new Department { name = "Support" });

        // Act
        var result = db.DepartmentTable.TryGet(row.id, out Row<Department> retrievedRow);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(retrievedRow.id, Is.EqualTo(row.id));
        Assert.That(retrievedRow.data.name, Is.EqualTo("Support"));
    }

    [Test]
    public void TryGet_FailsForNonExistentRow()
    {
        // Act
        var result = db.DepartmentTable.TryGet(999, out Row<Department> retrievedRow);

        // Assert
        Assert.That(result, Is.False);
        Assert.That(retrievedRow, Is.EqualTo(default(Row<Department>)));
    }

    [Test]
    public void Get_RetrievesRowSuccessfully()
    {
        // Arrange
        var row = db.DepartmentTable.Add(new Department { name = "Operations" });

        // Act
        var retrievedRow = db.DepartmentTable.Get(row.id);

        // Assert
        Assert.That(retrievedRow.id, Is.EqualTo(row.id));
        Assert.That(retrievedRow.data.name, Is.EqualTo("Operations"));
    }

    [Test]
    public void Test_UpdateAction()
    {
        var md = db.DepartmentTable.Add(new Department { name = "Marketing" });
        db.DepartmentTable.Add(new Department { name = "Sales" });
        var e = db.EmployeeTable.Add(new Employee { name = "Simon", department_id = md.id });
        
        db.EmployeeTable.Update((ref Row<Employee> emp) => emp.salary(100));
        // refresh the employee to get the updated value
        e = db.EmployeeTable.Get(e.id);
        
        Assert.That(e.salary(), Is.EqualTo(100));
    }
    
    [Test]
    public void Test_UpdateWhereAction()
    {
        var md = db.DepartmentTable.Add(new Department { name = "Marketing" });
        db.DepartmentTable.Add(new Department { name = "Sales" });
        db.EmployeeTable.Add(new Employee { name = "Simon", department_id = md.id });
        
        db.DepartmentTable.Update((ref Row<Department> r) => r.name($"{r.name()} Dept."), (in Row<Department> r) => r.name().StartsWith("Sal"));

        foreach (var id in db.DepartmentTable)
        {
            var row = db.DepartmentTable.Get(id);
            if (row.name().StartsWith("Sal"))
            {
                Assert.That(row.name(), Does.EndWith("Dept."));
            }
        }
    }
    
    
    [Test]
    public void Test_RemoveWhereAction()
    {
        db.DepartmentTable.Add(new Department { name = "Marketing" });
        db.DepartmentTable.Add(new Department { name = "Sales" });
        
        db.DepartmentTable.Remove((in Row<Department> r) => r.name().StartsWith("S"));
        
        Assert.That(db.DepartmentTable.Count, Is.EqualTo(1));
        
        foreach (var id in db.DepartmentTable)
        {
            var row = db.DepartmentTable.Get(id);
            Assert.That(row.name(), Is.Not.StartWith("S"));
        }
    }

    [Test]
    public void Test_CascadeDelete()
    {
        var md = db.DepartmentTable.Add(new Department {name = "Marketing"});
        db.DepartmentTable.Add(new Department {name = "Sales"});
        db.EmployeeTable.Add(new Employee {name = "Simon", department_id = md.id});
        
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => db.DepartmentTable.Remove(in md));
        
        db.DepartmentTable.Remove(md, CascadeOperation.Delete);
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(0));
        
    }
    
    [Test]
    public void Test_CascadeSetNull()
    {
        var md = db.DepartmentTable.Add(new Department {name = "Marketing"});
        db.DepartmentTable.Add(new Department {name = "Sales"});
        var e = db.EmployeeTable.Add(new Employee {name = "Simon", department_id = md.id});
        
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
        Assert.Throws<InvalidOperationException>(() => db.DepartmentTable.Remove(in md));
        
        Assert.That(e.department_id(), Is.EqualTo(md.id));
        
        db.DepartmentTable.Remove(md, CascadeOperation.SetNull);
        Assert.That(db.EmployeeTable.Count, Is.EqualTo(1));
        e = db.EmployeeTable.Get(e.id);
        Assert.That(e.department_id(), Is.Zero);
        
    }




}