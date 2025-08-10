using System;
using System.Collections.Generic;

namespace IntegrityTables;

public class UniqueIndex<T>
{
    private Dictionary<T, int> _dict = new();
    
    public void AssertDoesNotContain(T value)
    {
        if (value == null) return;
        if (_dict.ContainsKey(value))
            throw new InvalidOperationException($"Value {value} already exists in the index.");
    }
    
    public void Add(T value, int id)
    {
        if (value == null) return;
        _dict[value] = id;
    }
    
    public void Remove(T value)
    {
        if (value == null) return;
        _dict.Remove(value);
    }

    public bool TryGetValue(T value, out int id)
    {
        if (value == null)
        {
            id = -1;
            return false;
        }
        return _dict.TryGetValue(value, out id);
    }
}