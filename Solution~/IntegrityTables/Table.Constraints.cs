using System;
using System.Collections.Generic;

namespace IntegrityTables;

public partial class Table<T>
{
    private readonly List<(RowConditionFunc<T> func, string name)> _constraints = new();

    public void AddConstraint(RowConditionFunc<T> func, string name)
    {
        using(_lock.WriteScope())
        {
            _constraints.Add((func, name));
        }
        
    }

    private void CheckConstraints(in Row<T> row)
    {
        foreach (var (constraint, name) in _constraints)
            if (constraint(in row) == false)
                RaiseException(new InvalidOperationException($"Constraint '{name}` failed for row {row.id} in table {Name} ({constraint.Method.Name})"));
    }
}