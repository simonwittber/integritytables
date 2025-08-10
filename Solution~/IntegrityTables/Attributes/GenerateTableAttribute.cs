using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Struct)]
public class GenerateTableAttribute : Attribute
{
    public Type DBType;
    public string? GroupName;
    public bool Blittable;
    public int Capacity;
    
    public bool GenerateViewModel;
    public System.Type? GenerateEnum;

    public GenerateTableAttribute(Type dbType)
    {
        DBType = dbType;
    }

}