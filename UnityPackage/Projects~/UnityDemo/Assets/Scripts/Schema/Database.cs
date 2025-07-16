using System;
using IntegrityTables;

[GenerateDatabase]
public partial class Database {
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Player
{
    [Unique] 
    public string name;
    public int score;
    
    [DefaultData]
    public static Player[] DefaultData() => new Player[] {
        new Player { name = "Alice", score = 100 },
        new Player { name = "Bob", score = 150 },
        new Player { name = "Charlie", score = 200 },
    };
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Inventory
{
    [Reference(typeof(Player), NotNull=true, CollectionName = "InventoryItems"), Unique("player-item")]
    public int playerId;
    
    [Reference(typeof(Item), NotNull = true, PropertyName = nameof(Item)), Unique("player-item")]
    public int itemId;
    
    public int count;
    
    [CheckConstraint]
    public static bool IsValid(in Row<Inventory> row) => row.data.count >= 0;
}

[GenerateTable(typeof(Database)), Serializable]
public partial struct Item
{
    [Unique] 
    public string name;

    [DefaultData]
    public static Item[] DefaultData() => new Item[]
    {
        new Item { name = "Sword" },
        new Item { name = "Shield" },
        new Item { name = "Potion" },
    };

}


