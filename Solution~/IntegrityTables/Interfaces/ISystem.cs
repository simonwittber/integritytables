namespace IntegrityTables;

public interface ISystem<T> 
{
    public T database { get; set; }
    public bool AllowThreadedExecution { get; }
    public void Execute() { }
}