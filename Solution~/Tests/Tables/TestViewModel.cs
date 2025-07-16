namespace IntegrityTables.Tests;

[TestFixture]
public class TestViewModel
{
    private HumanResourcesDatabase db;
    private HumanResourcesDatabaseViewModelManager manager;

    [TearDown]
    public void TearDown()
    {
        manager.Dispose();
    }
    [SetUp]
    public void Setup()
    {
        db = new HumanResourcesDatabase();
        manager = new HumanResourcesDatabaseViewModelManager(db);
    }
    
    [Test]
    public void TestViewModelGet()
    {
        using var scope = db.CreateContext();
        
        var e1 = db.EmployeeTable.Add(new Employee { name = "Alice" });

        var vm1 = manager.EmployeeViewModels.Get(e1.id);
        
        Assert.That(vm1.Name.Value, Is.EqualTo("Alice"));
        var name = vm1.Name.Value;
        vm1.Name.OnChanged += (oldName, newName) =>
        {
            name = newName;
        };
        e1.name("Alice Smith");
        db.EmployeeTable.Update(ref e1);
        
        Assert.That(name, Is.EqualTo("Alice Smith"));
        Assert.That(vm1.Name.Value, Is.EqualTo("Alice Smith"));
    }

    [Test]
    public void TestViewModelCollections()
    {
        var e1 = db.EmployeeTable.Add(new Employee { name = "Alice" });
        var e2 = db.EmployeeTable.Add(new Employee { name = "Bob", boss_id = e1.id});
        var e3 = db.EmployeeTable.Add(new Employee { name = "Charlie", boss_id = e1.id });
        var vm1 = manager.EmployeeViewModels.Get(e1.id);
        Assert.That(vm1.Underlings.Count, Is.EqualTo(2));
        Assert.That(vm1.Underlings[0].Name.Value, Is.EqualTo("Bob"));
        Assert.That(vm1.Underlings[1].Name.Value, Is.EqualTo("Charlie"));
    }
    
    [Test]
    public void TestViewModelCollectionsRemove()
    {
        var e1 = db.EmployeeTable.Add(new Employee { name = "Alice" });
        var e2 = db.EmployeeTable.Add(new Employee { name = "Bob", boss_id = e1.id});
        var e3 = db.EmployeeTable.Add(new Employee { name = "Charlie", boss_id = e1.id });
        var vm1 = manager.EmployeeViewModels.Get(e1.id);
        e3.boss_id(0);
        db.EmployeeTable.Update(ref e3);
        Assert.That(vm1.Underlings.Count, Is.EqualTo(1));
        Assert.That(vm1.Underlings[0].Name.Value, Is.EqualTo("Bob"));
    }
    
    [Test]
    public void TestViewModelProperties()
    {
        var e1 = db.EmployeeTable.Add(new Employee { name = "Alice" });
        var e2 = db.EmployeeTable.Add(new Employee { name = "Bob", boss_id = e1.id});
        var e3 = db.EmployeeTable.Add(new Employee { name = "Charlie", boss_id = e1.id });
        var vm1 = manager.EmployeeViewModels.Get(e2.id);
        var vm2 = vm1.Boss;
        Assert.That(vm2.Name.Value, Is.EqualTo("Alice"));
        Assert.That(vm2.Underlings.Count, Is.EqualTo(2));
        

    }
    
    [Test]
    public void TestViewModelCollectionCaching()
    {
        var e1 = db.EmployeeTable.Add(new Employee { name = "Alice" });
        var e2 = db.EmployeeTable.Add(new Employee { name = "Bob", boss_id = e1.id});
        var e3 = db.EmployeeTable.Add(new Employee { name = "Charlie", boss_id = e1.id });
        var e4 = db.EmployeeTable.Add(new Employee { name = "Loner" });
        
        // 0 exist until we request one.
        Assert.That(manager.EmployeeViewModels.Count, Is.Zero);
        
        var vm1 = manager.EmployeeViewModels.Get(e2.id);
        // 1 will exist after we request one.
        Assert.That(manager.EmployeeViewModels.Count, Is.EqualTo(1));
        // The same vm1 should be returned when we request it again.
        var vm2 = manager.EmployeeViewModels.Get(e2.id);
        Assert.That(vm1, Is.SameAs(vm2));
        
        // When we request the boss, we will get another VM, and all the underlings will also be cached bringing total to 3.
        var vm3 = vm1.Boss;
        Assert.That(manager.EmployeeViewModels.Count, Is.EqualTo(3));
        
        // when we get the boss by a different route, it should be the same instance.
        var vm4 = manager.EmployeeViewModels.Get(e1.id);
        Assert.That(vm3, Is.SameAs(vm4));

    }
}
