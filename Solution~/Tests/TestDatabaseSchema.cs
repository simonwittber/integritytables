using IntegrityTables;

namespace Tests;

[GenerateDatabase]
public partial class Database
{
    
}

[GenerateTable(typeof(Database))]
public partial struct Employee
{
    [RequiredReference, PropertyName(nameof(Department)), CollectionName("Employees")]
    public Reference<Department> departmentId;
    
    [PropertyName(nameof(Location))]
    public Reference<Location> locationId;
    
    [BeforeUpdate(nameof(BeforeNameUpdate)), AfterUpdate(nameof(AfterNameUpdate))]
    public string name;
    
    public int age;
    
    public static void AfterNameUpdate(Database db, EmployeeRow row, string oldValue)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = row.name,
            action = "AfterNameUpdate"
        });
    }
    
    public static void BeforeNameUpdate(Database db, EmployeeRow row, string newValue)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = row.name,
            action = "BeforeNameUpdate"
        });
    }

    public static void BeforeAdd(Database db, Employee row)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = row.name,
            action = "BeforeAdd"
        });
    }
    
    public static void AfterAdd(Database db, EmployeeRow row)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = row.name,
            action = "AfterAdd"
        });
    }
    
    public static void BeforeRemove(Database db, EmployeeRow row)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = row.name,
            action = "BeforeRemove"
        });
    }
    
    public static void AfterRemove(Database db, int id)
    {
        db.EmployeeLogTable.Add(new EmployeeLog()
        {
            name = id.ToString(),
            action = "AfterRemove"
        });
    }
}

[GenerateTable(typeof(Database))]
public struct EmployeeLog
{
    public string name;
    public string action;
}

[GenerateTable(typeof(Database))]
public partial struct Department
{
    [Unique]
    public string name;
}

[GenerateTable(typeof(Database))]
public partial struct Location
{
    [Unique("state-name")]
    public string state;
    [Unique("state-name")]
    public string name;
}
