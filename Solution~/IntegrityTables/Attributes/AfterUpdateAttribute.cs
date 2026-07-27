using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public class AfterUpdateAttribute : Attribute
{
    public string MethodName { get; }
    
    public AfterUpdateAttribute(string methodName)
    {
        MethodName = methodName;
    }
}