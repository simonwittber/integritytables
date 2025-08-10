using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class PropertyNameAttribute : Attribute
{
    public string Name { get; }

    public PropertyNameAttribute(string name)
    {
        Name = name;
    }
}