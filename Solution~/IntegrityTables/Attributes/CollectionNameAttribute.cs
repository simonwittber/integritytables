using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class CollectionNameAttribute : Attribute
{
    public string Name { get; }

    public CollectionNameAttribute(string name)
    {
        Name = name;
    }
}