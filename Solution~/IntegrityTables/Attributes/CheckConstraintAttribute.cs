using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Method)]
public class CheckConstraintAttribute : Attribute
{
}