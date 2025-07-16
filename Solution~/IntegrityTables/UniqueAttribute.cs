using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class UniqueAttribute : Attribute
{
    public string? Name;

    public UniqueAttribute(string? name = null)
    {
        Name = name;
    }
}