using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class IgnoreForEqualityAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method)]
public class ValidateAttribute : Attribute
{
    private string FieldName;
    public ValidateAttribute(string fieldName)
    {
        FieldName = fieldName;
    }
}