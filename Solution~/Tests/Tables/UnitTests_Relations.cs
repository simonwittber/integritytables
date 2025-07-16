namespace IntegrityTables.Tests;

public class TestsRelations
{
    private HumanResourcesDatabase db;

    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
    }

    [Test]
    public void TestNotNull()
    {
        using var scope = db.CreateContext();
        Assert.Throws<InvalidOperationException>(() => db.AddressTable.Add(new Address()));
        var e = db.EmployeeTable.Add(new Employee());
        Assert.DoesNotThrow(() =>
        {
            db.AddressTable.Add(new Address
            {
                employee_id = e.id,
                address = "123 Main St"
            });
        });
        Assert.That(db.AddressTable.Count, Is.EqualTo(1));
        var addresses = e.Addresses().ToList();
        
        Assert.That(db.AddressTable.Get(addresses[0]).address(), Is.EqualTo("123 Main St"));
        
        Assert.Throws<InvalidOperationException>(() => db.EmployeeTable.Remove(e, CascadeOperation.SetNull));

        Assert.Throws<InvalidOperationException>(() => db.AddressTable.Update((ref Row<Address> row) => row.employee_id(0)));
        
        Assert.DoesNotThrow(() => db.EmployeeTable.Get(e.id));
        
        addresses = e.Addresses().ToList();
        Assert.That(addresses.Count, Is.EqualTo(1));
        Assert.That(db.AddressTable.Get(addresses[0]).address(), Is.EqualTo("123 Main St"));
        Assert.That(db.AddressTable.Get(addresses[0]).Employee().id, Is.EqualTo(e.id));
    }

    [Test]
    public void TestAddForNonExistingRelation()
    {
        Assert.Throws<InvalidOperationException>(() =>
        {
            db.EmployeeTable.Add(new Employee
            {
                name = "Simon",
                department_id = 1
            });
        });
    }

    [Test]
    public void TestAddForExistingRelationThenRemoveReference()
    {
        var d = db.DepartmentTable.Add(new Department {name = "HR"});
        Assert.DoesNotThrow(() =>
        {
            db.EmployeeTable.Add(new Employee
            {
                name = "Simon",
                department_id = d.id
            });
        });
        Assert.Throws<InvalidOperationException>(() => { db.DepartmentTable.Remove(in d); });
    }

    [Test]
    public void TestModifyReferenceToInvalid()
    {
        var id = 2;
        db.EmployeeTable.Exists((in Row<Employee> r) => r.data.boss_id == id);
        var d = db.DepartmentTable.Add(new Department {name = "HR"});

        var e = db.EmployeeTable.Add(new Employee
        {
            name = "Simon",
            department_id = d.id
        });
        Assert.DoesNotThrow(() =>
        {
            // does not throw, 0 is equivalent to null
            e.data.department_id = 0;
            db.EmployeeTable.Update(ref e);
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            e.data.department_id = 99;
            db.EmployeeTable.Update(ref e);
        });
    }

    [Test]
    public void TestExtensionMethods()
    {
        var d = db.DepartmentTable.Add(new Department {name = "HR"});
        db.DepartmentTable.Add(new Department {name = "HRX"});

        db.EmployeeTable.Add(new Employee
        {
            name = "Simon",
            department_id = d.id
        });

        db.EmployeeTable.Add(new Employee
        {
            name = "Simon X"
        });
    }

    [Test]
    public void TestUpdateWhere()
    {
        var d0 = db.DepartmentTable.Add(new Department {name = "HR"});
        var d1 = db.DepartmentTable.Add(new Department {name = "HRX"});

        var e = db.EmployeeTable.Add(new Employee
        {
            name = "Simon",
            department_id = d0.id
        });
        e.data.department_id = d1.id;
        db.EmployeeTable.Update(ref e);
        e.data.department_id = d0.id;
        db.EmployeeTable.Update(ref e);
        e.data.department_id = 0;
        db.EmployeeTable.Update(ref e);
    }

    [Test]
    public void TestJoin()
    {
        // var hr = db.DepartmentTable.Add(new Department {name = "HR"});
        // var engineering = db.DepartmentTable.Add(new Department {name = "Engineering"});
        // db.EmployeeTable.Add(new Employee {name = "Simon", department_id = hr.id});
        // db.EmployeeTable.Add(new Employee {name = "Boris", department_id = engineering.id});
        // db.EmployeeTable.Add(new Employee {name = "Vlad", department_id = engineering.id});
        //
        // var join = db.DepartmentTable.Join(db.EmployeeTable, (ref Row<Department> dept, ref Row<Employee> emp) => emp.data.department_id == dept.id);
        // foreach (var (dept, emp) in join) Assert.That(emp.data.department_id, Is.EqualTo(dept.id));
    }

    [Test]
    public void TestJoinShortcut()
    {
        // var hr = db.DepartmentTable.Add(new Department {name = "HR"});
        // var engineering = db.DepartmentTable.Add(new Department {name = "Engineering"});
        // db.EmployeeTable.Add(new Employee {name = "Simon", department_id = hr.id});
        // db.EmployeeTable.Add(new Employee {name = "Boris", department_id = engineering.id});
        // db.EmployeeTable.Add(new Employee {name = "Vlad", department_id = engineering.id});
        //
        // var join = db.Join(db.DepartmentTable, db.EmployeeTable, (ref Row<Department> dept, ref Row<Employee> emp) => emp.data.department_id == dept.id);
        // foreach (var (dept, emp) in join) Assert.That(emp.data.department_id, Is.EqualTo(dept.id));
    }
    
    [Test]
    public void OperationsOutsideChangeSet_AreIndependent()
    {
        // Add a negative value → fails
        var d = db.DepartmentTable.Add(new Department {name = "HR"});
        db.EmployeeTable.Add(new Employee() { department_id = d.id });
        
        Assert.Throws<InvalidOperationException>(() => db.DepartmentTable.Remove(in d));

        // Table should still be empty and accept new valid adds
        Assert.That(db.DepartmentTable.Count, Is.EqualTo(1), "Failed remove should not leave partial state.");
        db.DepartmentTable.Add(new Department() { name = "XY" });
        Assert.That(db.DepartmentTable.Count, Is.EqualTo(2), "Valid add outside ChangeSet should succeed after a failure.");
    }
}