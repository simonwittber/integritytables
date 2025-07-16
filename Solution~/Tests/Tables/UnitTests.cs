namespace IntegrityTables.Tests;

public class Tests
{
    private HumanResourcesDatabase _db;

    [SetUp]
    public void Setup()
    {
        _db = new HumanResourcesDatabase();
    }

    [Test]
    public void DefaultDataLoads()
    {
        Assert.That(_db.DeskTable.Count, Is.EqualTo(2));
        Assert.That(_db.DeskTable.Get(1).name() , Is.EqualTo("Desk 1"));
        Assert.That(_db.DeskTable.Get(2).name() , Is.EqualTo("Desk 2"));
    }
    
    [Test]
    public void TestGetOne()
    {
        Assert.That(_db.DeskTable.Count, Is.EqualTo(2));
        var row = _db.DeskTable.GetOne(i => i.name == "Desk 1");
        Assert.That(row.name(), Is.EqualTo("Desk 1"));
        
        Assert.Throws<KeyNotFoundException>(() => _db.DeskTable.GetOne(i => i.name == "Desk X"));
    }
    
    [Test]
    public void TestTryGetOne()
    {
        Assert.That(_db.DeskTable.Count, Is.EqualTo(2));
        var test = _db.DeskTable.TryGetOne(i => i.name == "Desk 1", out var row);
        Assert.That(test, Is.True);
        Assert.That(row.name(), Is.EqualTo("Desk 1"));  
        
        test = _db.DeskTable.TryGetOne(i => i.name == "Desk X", out row);
        Assert.That(test, Is.False);
        Assert.That(row, Is.EqualTo(default(Row<Desk>)));
    }

    [Test]
    public void Load_InvalidReferences_ThrowsIntegrityException()
    {
        var invalidEmployeeRows = new[]
        {
            new Row<Employee> {data = new Employee {name = "John Doe", department_id = 999}}, // Invalid department_id
            new Row<Employee> {data = new Employee {name = "John Doe", department_id = 999}}, // Invalid department_id
            new Row<Employee> {data = new Employee {name = "Jane X", location_id = 0}}, // valid location_id
            new Row<Employee> {data = new Employee {name = "Jane Z", location_id = 1}} // valid location_id
        };

        var locations = new[]
        {
            new Row<Location> {id = 1, data = new Location {name = "Location 1"}} // valid location
        };
        var warningMessage = string.Empty;

        Warnings.OnWarning += (msg => warningMessage = msg);
        _db.EmployeeTable.Load(invalidEmployeeRows);
        _db.LocationTable.Load(locations);
        _db.ValidateIntegrity();
        Assert.That(warningMessage, Does.Contain("not a valid reference to Department"));
    }

    [Test]
    public void Load_ValidReferences_PassesIntegrityValidation()
    {
        var invalidEmployeeRows = new[]
        {
            new Row<Employee> {data = new Employee {name = "John Doe", department_id = 0}}, // valid department_id
            new Row<Employee> {data = new Employee {name = "John Doe", department_id = 1}}, // valid department_id
            new Row<Employee> {data = new Employee {name = "Jane X", location_id = 0}}, // valid location_id
            new Row<Employee> {data = new Employee {name = "Jane Z", location_id = 1}} // valid location_id
        };

        var locations = new[]
        {
            new Row<Location> {id = 1, data = new Location {name = "Location 1"}} // valid location
        };

        var departments = new[]
        {
            new Row<Department> {id = 1, data = new Department {name = "Department 1"}} // valid department
        };

        _db.EmployeeTable.Load(invalidEmployeeRows);
        _db.LocationTable.Load(locations);
        _db.DepartmentTable.Load(departments);
        Assert.DoesNotThrow(() => _db.ValidateIntegrity());
    }

    [Test]
    public void Load_WarnsOnConstraintViolation()
    {
        Warnings.OnWarning -= WarningsOnOnWarning;
        Warnings.OnWarning += WarningsOnOnWarning;

        var warningMsg = string.Empty;
        void WarningsOnOnWarning(string obj)
        {
            warningMsg = obj;
        }

        var locations = new[]
        {
            new Row<Location> {id = 1, data = new Location {name = "Location 1"}}, // valid location
            new Row<Location> {id = 2, data = new Location {name = "Location 1"}} // valid location
        };
        _db.LocationTable.Load(locations);
        Assert.That(warningMsg, Does.Contain("InvalidOperationException"));
    }

    [Test]
    public void Get_ThrowsForNonExistentKey()
    {
        Assert.Throws<KeyNotFoundException>(() => { _db.EmployeeTable.Get(0); });
    }

    [Test]
    public void UniqueConstraint_ThrowsOnDuplicateAddition()
    {
        Row<Location> a;
        Row<Location> b;
        using(var cs = _db.NewChangeSet())
        {
            a = _db.LocationTable.Add(new Location {name = "A"}); 
            b = _db.LocationTable.Add(new Location {name = "B"});
            cs.Commit();
        }
        
        // a and b are now in the database.
        using(var cs = _db.NewChangeSet())
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                // try and add a duplicate name, will fail.
                _db.LocationTable.Add(new Location {name = "A"});
            });

            cs.Rollback();
        }
        Assert.Throws<InvalidOperationException>(() =>
        {
            // try and add a duplicate name, will fail.
            _db.LocationTable.Add(new Location {name = "A"});
        });

        Assert.Throws<InvalidOperationException>(() =>
        {
            b.data.name = a.data.name;
            _db.LocationTable.Update(ref b);
        });
    }

    [Test]
    public void UniqueConstraint_AllowsRollbackOnViolation()
    {
        Row<Employee> e;
        Row<Location> a;
        Row<Employee> e2;
        Row<ServiceArea> sl;
        Row<ServiceArea> sl2;
        using (var cs = _db.NewChangeSet())
        {
            a = _db.LocationTable.Add(new Location {name = "A"});
            e = _db.EmployeeTable.Add(new Employee {name = "E"});
            e2 = _db.EmployeeTable.Add(new Employee {name = "E"});
            sl = _db.ServiceAreaTable.Add(new ServiceArea {employee_id = e.id, location_id = a.id});
            sl2 = _db.ServiceAreaTable.Add(new ServiceArea {employee_id = e2.id, location_id = a.id});
            cs.Commit();
        }

        using (var cs = _db.NewChangeSet()){
            Assert.Throws<InvalidOperationException>(() => { _db.ServiceAreaTable.Add(new ServiceArea {employee_id = e.id, location_id = a.id}); });
            cs.Rollback();
        }

        using (var cs = _db.NewChangeSet())
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                sl.data.employee_id = e2.id;
                _db.ServiceAreaTable.Update(ref sl);
            });

            cs.Rollback();
        }

        _db.ServiceAreaTable.Remove(in sl2);
        sl = _db.ServiceAreaTable.Get(sl.id);
        Assert.DoesNotThrow(() =>
        {
            sl.data.employee_id = e2.id;
            _db.ServiceAreaTable.Update(ref sl);
        });
    }

    [Test]
    public void Join_ReturnsEmptyForEmptyTables()
    {
        var join = _db.DepartmentTable.Joined(_db.EmployeeTable, (in Row<Department> dept, in Row<Employee> emp) => emp.data.department_id == dept.id);
        Assert.IsFalse(join.MoveNext());
    }

    [Test]
    public void Join_ReturnsEmptyForNoMatchingRows()
    {
        _db.DepartmentTable.Add(new Department {name = "HR"});
        _db.EmployeeTable.Add(new Employee {name = "Simon", department_id = 0});
        
        var join = _db.DepartmentTable.Joined(_db.EmployeeTable, (in Row<Department> dept, in Row<Employee> emp) => emp.data.department_id == dept.id);
        Assert.IsFalse(join.MoveNext());
    }

    [Test]
    public void Join_ReturnsMultipleMatches()
    {
        var hr = _db.DepartmentTable.Add(new Department {name = "HR"});
        _db.EmployeeTable.Add(new Employee {name = "Simon", department_id = hr.id});
        _db.EmployeeTable.Add(new Employee {name = "Boris", department_id = hr.id});
        
        var join = _db.DepartmentTable.Joined(_db.EmployeeTable, (in Row<Department> dept, in Row<Employee> emp) => emp.data.department_id == dept.id);
        var count = 0;
        foreach (var (dept, emp) in join)
        {
            Assert.That(emp.data.department_id, Is.EqualTo(dept.id));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public void Join_EnumeratorCanBeReset()
    {
        var hr = _db.DepartmentTable.Add(new Department {name = "HR"});
        _db.EmployeeTable.Add(new Employee {name = "Simon", department_id = hr.id});
        
        var join = _db.DepartmentTable.Joined(_db.EmployeeTable, (in Row<Department> dept, in Row<Employee> emp) => emp.data.department_id == dept.id);
        Assert.IsTrue(join.MoveNext());
        join.Reset();
        Assert.IsTrue(join.MoveNext());
    }


    [Test]
    public void SelfJoin_ReturnsMatchingRows()
    {
        _db.DepartmentTable.Add(new Department {name = "HR"});
        _db.DepartmentTable.Add(new Department {name = "Engineering"});
        
        var join = _db.DepartmentTable.Joined(_db.DepartmentTable, (in Row<Department> dept1, in Row<Department> dept2) => dept1.id == dept2.id);
        var count = 0;
        foreach (var (dept1, dept2) in join)
        {
            Assert.That(dept1.id, Is.EqualTo(dept2.id));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(2)); // Each row matches itself
    }

    [Test]
    public void Join_WithComplexCondition_ReturnsFilteredResults()
    {
        var hr = _db.DepartmentTable.Add(new Department {name = "HR"});
        _db.EmployeeTable.Add(new Employee {name = "Simon", department_id = hr.id});
        _db.EmployeeTable.Add(new Employee {name = "Boris", department_id = hr.id});
        
        var join = _db.DepartmentTable.Joined(_db.EmployeeTable, (in Row<Department> dept, in Row<Employee> emp) => emp.data.department_id == dept.id && emp.data.name.StartsWith("S"));
        var count = 0;
        foreach (var (_, emp) in join)
        {
            Assert.That(emp.data.name, Is.EqualTo("Simon"));
            count++;
        }
        
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void Add_AddsRowSuccessfully()
    {
        var row = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});

        var e = _db.EmployeeTable.Get(row.id);

        Assert.That(e.data.name, Is.EqualTo("Simon Says"));
        Assert.That(e.id, Is.Not.EqualTo(0));
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddRow_ThrowsOnDuplicateAddition()
    {
        Row<Employee> externalRow;
        using(var cs = _db.NewChangeSet())
        {
            var row = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
            externalRow = new Row<Employee> {id = row.id};
            cs.Commit();
        }

        using (var cs = _db.NewChangeSet())
        {
            Assert.Throws<InvalidOperationException>(() => { _db.EmployeeTable.Add(ref externalRow); });
            cs.Rollback();
        }

        using (var cs = _db.NewChangeSet())
        {
            externalRow.id = 2;
            _db.EmployeeTable.Add(ref externalRow);
            _db.EmployeeTable.Add(new Employee {name = "Simon Says X"});
            cs.Commit();
        }

        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(3));
    }

    [Test]
    public void Iterate_SumsSalariesCorrectly()
    {
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        var total = 0f;
        foreach (var id in _db.EmployeeTable)
        {
            var e = _db.EmployeeTable.Get(id);
            total += e.data.salary;
        }

        Assert.That(total, Is.EqualTo(6));
    }

    [Test]
    public void MultiIterate_HandlesQueryCorrectly()
    {
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        var total = 0f;
        var q = _db.EmployeeTable.Query((in Employee row) => row.salary <= float.MaxValue);

        foreach (var e in q)
        {
            total += e.data.salary;
            if (total > 3) break;
        }

        Assert.That(total, Is.EqualTo(4));
        _db.EmployeeTable.Add(new Employee {name = "Simon Says", salary = 1});
        foreach (var e in _db.EmployeeTable.Query((in Employee row) => row.salary <= float.MaxValue)) 
            total += e.data.salary;

        Assert.That(total, Is.EqualTo(11));
    }

    [Test]
    public void Update_UpdatesRowSuccessfully()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        e.data.name = "Simon Says X";
        _db.EmployeeTable.Update(ref e);
        e = _db.EmployeeTable.Get(e.id);
        Assert.That(e.data.name, Is.EqualTo("Simon Says X"));
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(1));
    }

    [Test]
    public void Remove_RemovesRowSuccessfully()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        Assert.That(e._index, Is.GreaterThan(-1));
        _db.EmployeeTable.Remove(in e);
        Assert.Throws<KeyNotFoundException>(() => { e = _db.EmployeeTable.Get(e.id); });
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(0));
    }

    [Test]
    public void RemoveThenUpdate_ThrowsForRemovedRow()
    {
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        _db.EmployeeTable.Remove(in e);
        Assert.Throws<KeyNotFoundException>(() => { _db.EmployeeTable.Update(ref e); });
        Assert.That(_db.EmployeeTable.Count, Is.EqualTo(0));
    }

    [Test]
    public void StaleUpdate_ThrowsForStaleRow()
    {
        _db.EmployeeTable.BeginChangeSet();
        var e = _db.EmployeeTable.Add(new Employee {name = "Simon Says"});
        e.data.name = "Simon Says X";
        var oldE = e;
        _db.EmployeeTable.Update(ref e);
        _db.EmployeeTable.CommitChangeSet();
        using (var cs = _db.NewChangeSet())
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                // This will fail because it is a stale row.
                _db.EmployeeTable.Update(ref oldE);
            });
            Assert.Throws<InvalidOperationException>(() =>
            {
                // This will fail because the transaction was not rolled back
                _db.EmployeeTable.Add(new Employee {name = "Simon Says Z"});
            });
            cs.Rollback();
        }
    }

    [Test]
    public void QueryEnumerator_ReturnsAllMatchingRows()
    {
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 1"});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 2"});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 3"});
        var results = _db.EmployeeTable.Query((in Employee _) => true).ToArray();
        Assert.That(results.Length, Is.EqualTo(3));
        Assert.That(results[0].data.name, Is.EqualTo("Simon Says 1"));
        Assert.That(results[1].data.name, Is.EqualTo("Simon Says 2"));
        Assert.That(results[2].data.name, Is.EqualTo("Simon Says 3"));
    }

    [Test]
    public void QueryEnumeratorWithMetadata_ReturnsCorrectValues()
    {
        var metadata = _db.EmployeeTable.Metadata;
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 1"});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 2"});
        _db.EmployeeTable.Add(new Employee {name = "Simon Says 3"});
        var results = _db.EmployeeTable.Query((in Employee _) => true).ToArray();
        Assert.That(results.Length, Is.EqualTo(3));
        Assert.That(metadata.Get(results[0], 1), Is.EqualTo("Simon Says 1"));
        Assert.That(results[1].data.name, Is.EqualTo("Simon Says 2"));
        Assert.That(results[2].data.name, Is.EqualTo("Simon Says 3"));
    }

    [Test]
    public void QueryEnumerator_FiltersByNameWithoutException()
    {
        var table = new Table<Employee>();
        table.Add(new Employee {name = "Alice"});
        table.Add(new Employee {name = "Bob"});
        table.Add(new Employee {name = "Charlie"});

        Assert.DoesNotThrow(() =>
        {
            var results = table
                .Query((in Row<Employee> row) => row.name().StartsWith("A"))
                .ToArray();

            Assert.That(results.Length, Is.EqualTo(1));
            Assert.That(results[0].data.name, Is.EqualTo("Alice"));
        });
    }

    [Test]
    public void TryUpdate_UpdatesRowSuccessfully()
    {
        var row = _db.EmployeeTable.Add(new Employee {name = "Alice", salary = 5000});
        row.data.salary = 6000;

        var result = _db.EmployeeTable.TryUpdate(ref row);

        Assert.That(result, Is.True);
        var updatedRow = _db.EmployeeTable.Get(row.id);
        Assert.That(updatedRow.data.salary, Is.EqualTo(6000));
    }

    [Test]
    public void TryUpdate_FailsForNonExistentRow()
    {
        var row = new Row<Employee> {id = 999, data = new Employee {name = "NonExistent"}};

        var result = _db.EmployeeTable.TryUpdate(ref row);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryAdd_AddsRowSuccessfully()
    {
        var row = new Row<Employee> {data = new Employee {name = "Bob", salary = 4000}};

        var result = _db.EmployeeTable.TryAdd(ref row);

        Assert.That(result, Is.True);
        Assert.That(_db.EmployeeTable.Get(row.id).data.name, Is.EqualTo("Bob"));
    }

    [Test]
    public void TryAdd_FailsForDuplicateRow()
    {
        var row = _db.EmployeeTable.Add(new Employee {name = "Charlie"});
        var duplicateRow = new Row<Employee> {id = row.id, data = row.data};

        var result = _db.EmployeeTable.TryAdd(ref duplicateRow);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TryRemove_RemovesRowSuccessfully()
    {
        var row = _db.EmployeeTable.Add(new Employee {name = "Diana"});

        var result = _db.EmployeeTable.TryRemove(in row);

        Assert.That(result, Is.True);
        Assert.Throws<KeyNotFoundException>(() => _db.EmployeeTable.Get(row.id));
    }

    [Test]
    public void TryRemove_FailsForNonExistentRow()
    {
        var row = new Row<Employee> {id = 999, data = new Employee {name = "NonExistent"}};

        var result = _db.EmployeeTable.TryRemove(in row);

        Assert.That(result, Is.False);
    }
    
    [Test]
    public void TryGet_RetrievesRowSuccessfully()
    {
        var row = _db.EmployeeTable.Add(new Employee { name = "Alice", salary = 5000 });

        var result = _db.EmployeeTable.TryGet(row.id, out var retrievedRow);

        Assert.That(result, Is.True);
        Assert.That(retrievedRow.id, Is.EqualTo(row.id));
        Assert.That(retrievedRow.data.name, Is.EqualTo("Alice"));
    }

    [Test]
    public void TryGet_FailsForNonExistentRow()
    {
        var result = _db.EmployeeTable.TryGet(999, out var retrievedRow);

        Assert.That(result, Is.False);
        Assert.That(retrievedRow, Is.EqualTo(default(Row<Employee>)));
    }

    [Test]
    public void TryGet_RefOverloadRetrievesRowSuccessfully()
    {
        var row = _db.EmployeeTable.Add(new Employee { name = "Bob", salary = 4000 });
        var rowToRetrieve = new Row<Employee> { id = row.id };

        var result = _db.EmployeeTable.TryGet(rowToRetrieve.id, out rowToRetrieve);

        Assert.That(result, Is.True);
        Assert.That(rowToRetrieve.id, Is.EqualTo(row.id));
        Assert.That(rowToRetrieve.data.name, Is.EqualTo("Bob"));
    }

    [Test]
    public void TryGet_RefOverloadFailsForNonExistentRow()
    {
        var rowToRetrieve = new Row<Employee> { id = 999 };

        var result = _db.EmployeeTable.TryGet(rowToRetrieve.id, out rowToRetrieve);

        Assert.That(result, Is.False);
        Assert.That(rowToRetrieve, Is.EqualTo(default(Row<Employee>)));
    }

    [TestFixture]
    public class TableSortTests
    {
        private Table<Employee> _table;

        [SetUp]
        public void Setup()
        {
            _table = new Table<Employee>();
            _table.Add(new Employee {name = "Charlie", salary = 3000});
            _table.Add(new Employee {name = "Alice", salary = 5000});
            _table.Add(new Employee {name = "Bob", salary = 4000});
        }

        [Test]
        public void Sort_SortsTableBySalaryAscending()
        {
            // Act
            _table.Sort((a, b) => a.salary().CompareTo(b.salary()));

            // Assert
            var rows = _table.ToArray();
            Assert.That(rows[0].data.name, Is.EqualTo("Charlie"));
            Assert.That(rows[1].data.name, Is.EqualTo("Bob"));
            Assert.That(rows[2].data.name, Is.EqualTo("Alice"));
        }

        [Test]
        public void Sort_SortsTableBySalaryDescending()
        {
            // Act
            _table.Sort((a, b) => b.salary().CompareTo(a.salary()));

            // Assert
            var rows = _table.ToArray();
            Assert.That(rows[0].data.name, Is.EqualTo("Alice"));
            Assert.That(rows[1].data.name, Is.EqualTo("Bob"));
            Assert.That(rows[2].data.name, Is.EqualTo("Charlie"));
        }

        [Test]
        public void SortBy_SortsTableByNameAscendingAndSalaryDescending()
        {
            // Act
            _table.SortBy(
                key1: e => e.name(), desc1: false,
                key2: e => e.salary(), desc2: true
            );

            // Assert
            var rows = _table.ToArray();
            Assert.That(rows[0].data.name, Is.EqualTo("Alice"));
            Assert.That(rows[1].data.name, Is.EqualTo("Bob"));
            Assert.That(rows[2].data.name, Is.EqualTo("Charlie"));
        }

        [Test]
        public void SortBy_SortsTableByMultipleKeys()
        {
            // Arrange
            _table.Add(new Employee {name = "Alice", salary = 4500});

            // Act
            _table.SortBy(
                e => e.name(), false,
                e => e.salary(), false
            );

            // Assert
            var rows = _table.ToArray();
            Assert.That(rows[0].data.name, Is.EqualTo("Alice"));
            Assert.That(rows[0].data.salary, Is.EqualTo(4500));
            Assert.That(rows[1].data.name, Is.EqualTo("Alice"));
            Assert.That(rows[1].data.salary, Is.EqualTo(5000));
            Assert.That(rows[2].data.name, Is.EqualTo("Bob"));
            Assert.That(rows[3].data.name, Is.EqualTo("Charlie"));
        }


        [Test]
        public void TestOperationsWhileIteratingRows()
        {
            // structural modification is not allowed while iterating over rows.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var i in _table.Rows())
                {
                    if (i.data.name == "Charlie")
                    {
                        _table.Remove(i.id);
                    }
                }
            });
            
            // structural modification is not allowed while iterating over rows.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var i in _table.Rows())
                {
                    _table.Add(new Employee {name = "X", salary = 4500});
                }
            });
            
            // modifying row data is allowed.
            Assert.DoesNotThrow(() =>
            {
                foreach (var row in _table.Rows())
                {
                    var rowCopy = _table.Get(row.id);
                    rowCopy.data.salary = 4500;
                    _table.Update(ref rowCopy);
                }
            });
            
        }
        
        [Test]
        public void TestOperationsWhileIteratingIds()
        {
            // structural modification is not allowed while iterating over ids.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var id in _table)
                {
                    if (id == 1)
                    {
                        _table.Remove(id);
                    }
                }
            });
            
            // structural modification is not allowed while iterating over ids.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var id in _table)
                {
                    _table.Add(new Employee {name = "X", salary = 4500});
                }
            });
            
            // modifying row data is allowed.
            Assert.DoesNotThrow(() =>
            {
                foreach (var id in _table)
                {
                    var row = _table.Get(id);
                    row.data.salary = 45000;
                    _table.Update(ref row);
                }
            });
            
        }
        
        [Test]
        public void TestOperationsWhileIteratingQuery()
        {
            // structural modification is not allowed while iterating over ids.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var row in _table.Query((in Row<Employee> row) => row.salary() > 0))
                {
                    _table.Remove(row.id);
                }
            });
            
            // structural modification is not allowed while iterating over ids.
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (var row in _table.Query((in Row<Employee> row) => row.salary() > 0))
                {
                    _table.Add(new Employee {name = "X", salary = 4500});
                }
            });
            
            // modifying row data is allowed.
            Assert.DoesNotThrow(() =>
            {
                foreach (var row in _table.Query((in Row<Employee> row) => row.salary() > 0))
                {
                    var rowCopy = _table.Get(row.id);
                    rowCopy.data.salary = 45000;
                    _table.Update(ref rowCopy);
                }
            });
            
        }
    }
}
