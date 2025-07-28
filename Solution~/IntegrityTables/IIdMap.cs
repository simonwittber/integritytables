namespace IntegrityTables;

public interface IIdMap
{
    void Remove(int id);
    int this[int id] { get; set; }
    void Clear();
    bool ContainsKey(int id);
    bool TryGetValue(int id, out int index);
}