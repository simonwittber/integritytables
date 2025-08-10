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
    public Reference<Department> departmentId = -1;
    
    [PropertyName(nameof(Location))]
    public Reference<Location> locationId = -1;
    
    [BeforeUpdate(nameof(BeforeNameUpdate)), AfterUpdate(nameof(AfterNameUpdate))]
    public string name;
    
    public int age;

    public Employee()
    {
        name = null;
        age = 0;
    }

    public static void AfterNameUpdate(Database db, EmployeeRow row, string oldValue)
    {
        db.EmployeeLogTable.Add(
            name : row.name,
            action : "AfterNameUpdate"
        );
    }
    
    public static void BeforeNameUpdate(Database db, EmployeeRow row, string newValue)
    {
        db.EmployeeLogTable.Add(
            name : row.name,
            action : "BeforeNameUpdate"
        );
    }

    public static void BeforeAdd(Database db, int departmentId, int locationId, string name, int age)
    {
        db.EmployeeLogTable.Add(
            name : name,
            action : "BeforeAdd"
        );
    }
    
    public static void AfterAdd(Database db, EmployeeRow row)
    {
        db.EmployeeLogTable.Add(
            name : row.name,
            action : "AfterAdd"
        );
    }
    
    public static void BeforeRemove(Database db, EmployeeRow row)
    {
        db.EmployeeLogTable.Add(
            name : row.name,
            action : "BeforeRemove"
        );
    }
    
    public static void AfterRemove(Database db, int id)
    {
        db.EmployeeLogTable.Add(
            name : id.ToString(),
            action : "AfterRemove"
        );
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
    [Unique]
    public string state;
    [Unique]
    public string name;
}
