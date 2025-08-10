using IntegrityTables;

namespace Tests;

[GenerateDatabase]
public partial class Database
{
    
}

[GenerateTable(typeof(Database))]
public partial struct Employee
{
    [RequiredReference]
    public Reference<Department> departmentId;
    public Reference<Location> locationId;
    public string name;
    public int age;
}

[GenerateTable(typeof(Database))]
public partial struct Department
{
    public string name;
}

[GenerateTable(typeof(Database))]
public partial struct Location
{
    public string name;
}
