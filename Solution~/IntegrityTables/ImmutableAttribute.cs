using System;

namespace IntegrityTables;

[AttributeUsage(AttributeTargets.Field)]
public class ImmutableAttribute : Attribute
{
    public ImmutableAttribute()
    {
    }
}