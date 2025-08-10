using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class ReferenceAttribute : Attribute
{
    public string? CollectionName;
    public string? PropertyName;
    public bool NotNull;
    public Type Type;
    public bool CreateIfMissing;
    public string? MatchField;

    public ReferenceAttribute(Type type)
    {
        Type = type;
    }

}
