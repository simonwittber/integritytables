using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public class BeforeUpdateAttribute : Attribute
{
    public string MethodName { get; }
    
    public BeforeUpdateAttribute(string methodName)
    {
        MethodName = methodName;
    }
}