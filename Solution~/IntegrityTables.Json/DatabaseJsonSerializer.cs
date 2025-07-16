using System;
using System.Collections.Generic;
using System.IO;

namespace IntegrityTables;
using System.Text.Json;

public class DatabaseJsonSerializer : IPersistence
{
    private readonly string _path;
    readonly JsonSerializerOptions _opts = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public DatabaseJsonSerializer(string path)
    {
        _path = path;
    }
    public void SaveTable<T>(ITable<T> table) where T : struct, IEquatable<T>
    {
        Directory.CreateDirectory(_path);
        // Grab a List<T> (fully typed)
        List<Row<T>> allRows = table.ToList();
        // Serialize with the real T
        string json = JsonSerializer.Serialize(allRows, _opts);
        SaveJson(table.Name, json);
    }

    protected virtual void SaveJson(string name, string json)
    {
        File.WriteAllText(
            Path.Combine(_path, name + ".json"),
            json
        );
    }

    public void LoadTable<T>(ITable<T> table) where T : struct, IEquatable<T>
    {
        var tableName = table.Name;
        if (!LoadJson(tableName, out var json)) 
            return;
        // Deserialize back into List<T>
        var list = JsonSerializer.Deserialize<List<Row<T>>>(json!, _opts);
        if (list == null)
            throw new InvalidOperationException($"Failed to load {tableName}");
        // Load the typed rows back into the table
        table.Load(list);
    }

    protected virtual bool LoadJson(string tableName, out string? json) 
    {
        var file = Path.Combine(_path, tableName + ".json");
        if (!File.Exists(file))
        {
            json = null;
            return false;
        }
        json = File.ReadAllText(file);
        return true;
    }
}