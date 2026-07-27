using System.Runtime.CompilerServices;

namespace IntegrityTables;

public readonly struct Reference<T>  where T : struct
{
    private readonly int _id = -1;

    public Reference(int id) => _id = id;

    public static implicit operator Reference<T>(int id) => new(id);

    public static implicit operator int(Reference<T> reference) => reference._id;
}