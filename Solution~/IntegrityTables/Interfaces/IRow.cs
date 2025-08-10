namespace IntegrityTables;

public interface IRow<T>
{
    public int id { get; }
    object this[int fieldIndex] { get; set; }
}