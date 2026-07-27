using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class GenerateDatabaseAttribute : Attribute
{
    public bool GenerateForUnity;
}