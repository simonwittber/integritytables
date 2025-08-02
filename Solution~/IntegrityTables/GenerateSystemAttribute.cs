using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Class)]
public class GenerateSystemAttribute : Attribute
{
    public GenerateSystemAttribute(Type dbType)
    {
    }
}
