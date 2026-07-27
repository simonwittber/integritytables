using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class UniqueAttribute : Attribute
{
    public UniqueAttribute()
    {
    }
}