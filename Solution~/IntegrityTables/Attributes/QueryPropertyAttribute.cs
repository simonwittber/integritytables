using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Method)]
public class QueryPropertyAttribute : Attribute
{
    public string Name;

    public QueryPropertyAttribute(string name)
    {
        Name = name;
    }
}