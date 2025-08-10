using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class GenerateServiceAttribute : Attribute
{
    public Type DBType;
    public GenerateServiceAttribute(Type dbType)
    {
        DBType = dbType;
    }
}