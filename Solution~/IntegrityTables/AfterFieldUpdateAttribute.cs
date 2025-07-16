using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class AfterFieldUpdateAttribute : Attribute
{
    public string FieldName { get; }

    public AfterFieldUpdateAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}