namespace IntegrityTables;

public interface IDatabase
{
    IPersistence Persistence { get; set; }
    void Save();
    void Load();
    public ITable[] Tables { get; }
}