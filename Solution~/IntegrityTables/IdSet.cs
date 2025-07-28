using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public class IdSet
{
    private PagedIdMap map = new();
    
    public bool Add(int value)
    {
        if (map.ContainsKey(value)) return false; // already exists
        map[value] = 1; // use 1 as a placeholder value
        return true;
    }

    public void IntersectWith(Span<int> span)
    {
        var newMap = new PagedIdMap();
        foreach (var value in span)
        {
            if (value < 0) continue; // skip negative values
            if (map.ContainsKey(value))
            {
                newMap[value] = 1; // use 1 as a placeholder value
            }
        }
        map = newMap; // replace the old map with the new one
            
    }

    public bool Contains(int p0)
    {
        return map.ContainsKey(p0);
    }

    public bool Remove(int value)
    {
        if (!map.ContainsKey(value)) return false; // does not exist
        map.Remove(value);
        return true;
    }

    public void UnionWith(Span<int> empty)
    {
        foreach (var value in empty)
        {
            if (value < 0) continue; // skip negative values
            map[value] = 1; // use 1 as a placeholder value
        }
    }
}