using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class BeforeFieldUpdateAttribute : Attribute
{
    public string FieldName { get; }

    public BeforeFieldUpdateAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}