using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class RequiredReferenceAttribute : Attribute
{
}