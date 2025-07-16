using System;
using IntegrityTables;
using UnityEngine;
using Random = UnityEngine.Random;

public class ScoreRandomizer : MonoBehaviour
{
    public DatabaseAsset databaseAsset;
    private Row<Player>[] players;
    private Row<Item>[] items;

    public Database database => databaseAsset?.database as Database;

    void Start()
    {
        players = database.PlayerTable.ToArray();
        items = database.ItemTable.ToArray();
    }
    
    void Update()
    {
        if (database == null) return;
        UpdateRandomScores();
        CreateAndRemoveRandomInventory();
    }

    private void CreateAndRemoveRandomInventory()
    {
        using var context = database.CreateContext();

        var playerId = players[Random.Range(0, players.Length)].id;
        var itemId = items[Random.Range(0, items.Length)].id;
        try
        {
            database.InventoryTable.Add(new Inventory() {playerId = playerId, itemId = itemId, count = Random.Range(1, 10)});
        }
        catch (InvalidOperationException)
        {
            database.InventoryTable.Remove((in Row<Inventory> i) => i.data.playerId == playerId && i.data.itemId == itemId);
        }
    }

    private void UpdateRandomScores()
    {
        using var context = database.CreateContext();

        foreach (var playerId in database.PlayerTable)
        {
            var row = database.PlayerTable.Get(playerId);
            row.data.score = Random.Range(0, 100);
            row.Save();
        }
    }
}
