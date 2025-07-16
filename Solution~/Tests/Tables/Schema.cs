using IntegrityTables;

[GenerateTable(typeof(HumanResourcesDatabase), GenerateViewModel = true), Serializable]
public partial struct Employee
{
    public string name;

    [Reference(typeof(Department), PropertyName = "GetDepartment", CollectionName = "GetEmployees")]
    public int department_id;

    [Reference(typeof(Employee), PropertyName = "Boss", CollectionName = "Underlings")]
    public int boss_id;

    [HotField] public float salary;

    [Reference(typeof(Location))] public int location_id;
    [Reference(typeof(Location))] public int service_location_id;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Friends
{
    [Reference(typeof(Employee), CollectionName = "Friends", NotNull = true), Unique("Friends")]
    public int employee_idA;

    [Reference(typeof(Employee), CollectionName = "Friends", NotNull = true), Unique("Friends")]
    public int employee_idB;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct EmployeeDesk
{
    [Unique("employee_id_desk_id")] [Reference(typeof(Employee), CollectionName = "Desks", NotNull = true)]
    public int employee_id;

    [Unique("employee_id_desk_id")] [Reference(typeof(Desk), CollectionName = "Employees", NotNull = true)]
    public int desk_id;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Desk
{
    [Unique] public string name;

    [DefaultData]
    public static Desk[] DefaultData()
    {
        return
        [
            new Desk() {name = "Desk 1"},
            new Desk() {name = "Desk 2"},
        ];
    }
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct ServiceArea
{
    [Reference(typeof(Employee))] [Unique("employee_id_location_id")]
    public int employee_id;

    [Reference(typeof(Location))] [Unique("employee_id_location_id")]
    public int location_id;
}

[GenerateTable(typeof(HumanResourcesDatabase), GenerateViewModel = true), Serializable]
public partial struct Department
{
    [Reference(typeof(Location))] public int location_id;
    [Unique] public string name;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Address
{
    [Reference(typeof(Employee), NotNull = true, CollectionName = "Addresses", PropertyName = "Employee")]
    public int employee_id;

    public string address;
}

[GenerateTable(typeof(HumanResourcesDatabase), GenerateViewModel = true), Serializable]
public partial struct Location
{
    [Unique] public string name;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Entity
{
}

[GenerateDatabase(GenerateForUnity = false)]
public partial class HumanResourcesDatabase
{
}

[GenerateTable(typeof(TriggerDB)), Serializable]
public partial struct TriggerTableA
{
    public bool flag;
    public string text;

    [BeforeAdd]
    public static void BeforeAdd(TriggerDB db, ref Row<TriggerTableA> row)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {text = row.data.text, beforeAdd = true});
    }

    [AfterAdd]
    public static void AfterAdd(TriggerDB db, in Row<TriggerTableA> row)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {b_id = row.id, text = row.data.text, afterAdd = true});
    }

    [BeforeUpdate]
    public static void BeforeUpdate(TriggerDB db, in Row<TriggerTableA> oldRow, ref Row<TriggerTableA> newRow)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {b_id = newRow.id, text = newRow.data.text, beforeUpdate = true});
    }

    [AfterUpdate]
    public static void AfterUpdate(TriggerDB db, in Row<TriggerTableA> oldRow, in Row<TriggerTableA> newRow)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {b_id = newRow.id, text = newRow.data.text, afterUpdate = true});
    }

    [BeforeRemove]
    public static void BeforeRemove(TriggerDB db, in Row<TriggerTableA> row)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {b_id = row.id, text = row.data.text, beforeRemove = true});
    }

    [AfterRemove]
    public static void AfterRemove(TriggerDB db, in Row<TriggerTableA> row)
    {
        db.TriggerTableBTable.Add(new TriggerTableB() {b_id = row.id, text = row.data.text, afterRemove = true});
    }
}

[GenerateTable(typeof(TriggerDB)), Serializable]
public partial struct TriggerTableB
{
    public string text;
    public int b_id;
    public bool beforeAdd, afterAdd, beforeUpdate, afterUpdate, beforeRemove, afterRemove;
}

[GenerateDatabase(GenerateForUnity = false)]
public partial class TriggerDB
{
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct GameLevel
{
    public int seed;
    public string name;

    [Reference(typeof(GameLevelType), NotNull = true, PropertyName = "LevelType", CollectionName = "Levels")]
    public int gameLevelTypeId;

    [AfterAdd]
    public static void AfterAdd(HumanResourcesDatabase db, in Row<GameLevel> gameLevel)
    {
        gameLevel.LevelType();
    }
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct GameLevelType
{
    public string name;
    public int depth;

    [AfterAdd]
    public static void AfterAdd(HumanResourcesDatabase db, in Row<GameLevelType> gameLevelType)
    {
        // db.Remove((in Row<GameLevel> row) => row.gameLevelTypeId() == typeId);
    }

    [AfterUpdate]
    public static void AfterUpdate(HumanResourcesDatabase db, in Row<GameLevelType> oldRow, in Row<GameLevelType> newRow)
    {
    }
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct ConstrainedTable
{
    public string name;

    [CheckConstraint]
    public static bool NameNotNull(in Row<ConstrainedTable> row)
    {
        return row.data.name != null;
    }
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Parent
{
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct Child
{
    [Reference(typeof(Parent))] public int parentId;
}

[GenerateTable(typeof(HumanResourcesDatabase)), Serializable]
public partial struct CascadeTableC
{
    [Reference(typeof(Child))] public int parentId;
}