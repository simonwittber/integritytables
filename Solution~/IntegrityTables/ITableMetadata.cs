namespace IntegrityTables;

public interface ITableMetadata
{
    string Group { get; }
    string[] Names { get; }
    System.Type[] Types { get; }
    System.Type[] ReferencedTypes { get; }
    bool IsBlittable { get; }
    bool IsComponent { get; }
    (System.Type type, string fieldName)[] ReferencingTypes { get; }
    int IndexOf(string fieldName);
    (int index, string name, System.Type type, System.Type referencedType) GetInfo(int index);
    int Count { get; }
    object Get(object data, int index);
    void Set(ref object data, int index, object value);
}

public interface ITableMetadata<T> : ITableMetadata where T : struct
{
    object Get(in T data, int index);
    void Set(ref T data, int index, object value);
}

public struct TableMetadataEnumerator
{
    private readonly ITableMetadata _metadata;
    private int _idx;

    public TableMetadataEnumerator(ITableMetadata metadata)
    {
        _metadata = metadata;
        _idx = -1;
    }

    public bool MoveNext() => ++_idx < _metadata.Names.Length;

    public (int index, string name, System.Type type, System.Type referencedType) Current => _metadata.GetInfo(_idx);
}

public static class TableMetadataExtensions
{
    public static TableMetadataEnumerator GetEnumerator(this ITableMetadata metadata) => new TableMetadataEnumerator(metadata);
}