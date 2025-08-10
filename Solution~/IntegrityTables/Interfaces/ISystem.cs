namespace IntegrityTables;

public interface ISystem<T> where T : IDatabase
{
    public T database { get; set; }

    public void Initialize(T database)
    {
        this.database = database;
    }

    public void Execute() { }
}