using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace IntegrityTables;

public class ModificationLog : IDisposable
{
    private DenseIdMap _modifiedIndexes;
    private Context<ModificationLog> _context;

    public static ModificationLog New(IRowContainer container)
    {
        var instance = ObjectPool<ModificationLog>.Get();
        instance._context = new Context<ModificationLog>(instance);
        return instance;
    }

    public ModificationLog()
    {
        _modifiedIndexes = new DenseIdMap();
        _context = null;
    }
    
    public void Add(int rowIndex)
    {
        var index = _modifiedIndexes.GetOrAdd(rowIndex);
    }
    
    public void Dispose()
    {
        _modifiedIndexes.Clear();
        _context.Dispose();
        _context = null!;
        ObjectPool<ModificationLog>.Return(this);
    }
}