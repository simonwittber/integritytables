using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace IntegrityTables.Tests;

[TestFixture]
public class QueryByIdEnumeratorTests
{
    private HumanResourcesDatabase _db;

    [SetUp]
    public void Setup()
    {
        _db = new HumanResourcesDatabase();
        
        // Add some test data and capture the actual IDs assigned
        var johnRow = _db.EmployeeTable.Add(new Employee { name = "John Doe" });
        var janeRow = _db.EmployeeTable.Add(new Employee { name = "Jane Smith" });
        var bobRow = _db.EmployeeTable.Add(new Employee { name = "Bob Johnson" });
        var aliceRow = _db.EmployeeTable.Add(new Employee { name = "Alice Brown" });
        
        // Store the actual IDs for use in tests
        JohnId = johnRow.id;
        JaneId = janeRow.id;
        BobId = bobRow.id;
        AliceId = aliceRow.id;
    }

    // Test data IDs - these will be set in Setup()
    private int JohnId;
    private int JaneId;
    private int BobId;
    private int AliceId;

    
    [Test]
    public void MoveNext_WithValidIds_ReturnsTrue()
    {
        var ids = new List<int> { JohnId, JaneId, BobId };
        var enumerator = new QueryByIdEnumerator<Employee>(_db.EmployeeTable, ids);
        
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current.data.name, Is.EqualTo("John Doe"));
        
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current.data.name, Is.EqualTo("Jane Smith"));
        
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current.data.name, Is.EqualTo("Bob Johnson"));
        
        Assert.That(enumerator.MoveNext(), Is.False);
    }

    [Test]
    public void Reset_AfterEnumeration_ResetsToBeginning()
    {
        var ids = new List<int> { JohnId, JaneId };
        var enumerator = new QueryByIdEnumerator<Employee>(_db.EmployeeTable, ids);
        
        // Enumerate to the end
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.MoveNext(), Is.False);
        
        // Reset and enumerate again
        enumerator.Reset();
        Assert.That(enumerator.MoveNext(), Is.True);
        Assert.That(enumerator.Current.data.name, Is.EqualTo("John Doe"));
    }

    [Test]
    public void ForeachEnumeration_WithValidIds_IteratesCorrectly()
    {
        var ids = new List<int> { JohnId, BobId, AliceId };
        var enumerator = new QueryByIdEnumerator<Employee>(_db.EmployeeTable, ids);
        
        var names = new List<string>();
        foreach (var row in enumerator)
        {
            names.Add(row.data.name);
        }
        
        Assert.That(names.Count, Is.EqualTo(3));
        Assert.That(names[0], Is.EqualTo("John Doe"));
        Assert.That(names[1], Is.EqualTo("Bob Johnson"));
        Assert.That(names[2], Is.EqualTo("Alice Brown"));
    }
    
}
