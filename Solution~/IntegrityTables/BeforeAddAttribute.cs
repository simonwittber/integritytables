using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class BeforeAddAttribute : Attribute
{
}