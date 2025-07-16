namespace DocumentationTests.EnumTablesAndDefaultData;

using IntegrityTables;

[GenerateDatabase]
public partial class Database
{
}

public enum ModuleTypes
{
    Medical = 1,
    Transporter = 2,
    Helm = 3,
}
// This table has GenerateEnum = typeof(ModuleTypes, which means it will generate rows for the ModuleType table,
// using the specified enum type. The ids of the rows will correspond to the values of the enum, so take care not
// to use 0 as that is reserved for null values.
[GenerateTable(typeof(Database), GenerateEnum = typeof(ModuleTypes)), Serializable]
public partial struct ModuleType
{
    [Unique] public string name;
    
    public int value;
    
    // When a table has GenerateEnum set, it can also have a static method marked with [ConfigureEnum].
    // This method is used to configure the row values when the table is created.
    [ConfigureEnum]
    public static void ConfigureEnum(ref Row<ModuleType> row)
    {
        switch ((ModuleTypes)row.id)
        {
            case ModuleTypes.Medical:
                row.data.value = 1;
                break;
            case ModuleTypes.Transporter:
                row.data.value = 2;
                break;
            case ModuleTypes.Helm:
                row.data.value = 3;
                break;
        }
    }
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Module
{
    [Reference(typeof(ModuleType), NotNull = true)]
    public int moduleTypeId;
}


// This table has a static method marked with [DefaultData]. This method is used to load
// the table rows when the database is initialized.
[GenerateTable(typeof(Database)), Serializable]
public partial struct Role
{
    public string name;
    public int priority;

    [DefaultData]
    public static Role[] CreateDefaultRoles()
    {
        return
        [
            new Role() { name = "Admin", priority = 0},
            new Role() { name = "Moderator", priority = 1},
            new Role() { name = "User", priority = 3},
            new Role() { name = "Banned", priority = 99},
        ];
    }
}

public class TestDocumentation
{
    [Test]
    public void TestMethod()
    {
        var db = new Database();

        // Test that the enums were created as rows in the table.
        foreach (var i in Enum.GetValues<ModuleTypes>())
        {
            // The GetBy method is generated for tables with GenerateEnum set.
            // It is just shorthand for Get((int)ModuleTypes)
            var enumRow = db.ModuleTypeTable.GetBy(i);
            Assert.That(enumRow.data.name, Is.EqualTo(i.ToString()));
            switch ((ModuleTypes)enumRow.id)
            {
                case ModuleTypes.Medical:
                    Assert.That(enumRow.data.value, Is.EqualTo(1));
                    break;
                case ModuleTypes.Transporter:
                    Assert.That(enumRow.data.value, Is.EqualTo(2));
                    break;
                case ModuleTypes.Helm:
                    Assert.That(enumRow.data.value, Is.EqualTo(3));
                    break;
            }
        }
        
        // Test that the default data was created in the table.
        Assert.That(db.RoleTable.Exists(i => i.name == "Admin" && i.priority == 0), Is.True);
        Assert.That(db.RoleTable.Exists(i => i.name == "User" && i.priority == 3), Is.True);
        Assert.That(db.RoleTable.Exists(i => i.name == "Moderator" && i.priority == 1), Is.True);
        Assert.That(db.RoleTable.Exists(i => i.name == "Banned" && i.priority == 99), Is.True);
    }
}