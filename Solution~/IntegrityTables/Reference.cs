using System;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public readonly struct Reference<T>  where T : struct
{
    private readonly int _id;

    public Reference(int id) => _id = id;

    public static implicit operator Reference<T>(int id) => new(id+1);

    public static implicit operator int(Reference<T> reference) => reference._id-1;
}
