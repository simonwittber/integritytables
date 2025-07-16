using System;
using System.Collections.Generic;

namespace IntegrityTables;

[Serializable]
public struct Row<T> : IEquatable<Row<T>> where T : IEquatable<T>
{
    public int id;
    public int _index;
    public int _version;
    public T data;

    public Row(int id, T data)
    {
        this.id = id;
        this.data = data;
        _index = -1;
        _version = 0;
    }

    public override string ToString()
    {
        return $"{typeof(T).Name}(id:{id}, {data})";
    }

    public bool Equals(Row<T> other)
    {
        return id == other.id && _index == other._index && _version == other._version && EqualityComparer<T>.Default.Equals(data, other.data);
    }

    public override bool Equals(object? obj)
    {
        return obj is Row<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(id, _index, _version, data);
    }

    public static bool operator ==(Row<T> left, Row<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Row<T> left, Row<T> right)
    {
        return !left.Equals(right);
    }
}

