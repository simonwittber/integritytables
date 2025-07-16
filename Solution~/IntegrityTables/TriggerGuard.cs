using System;
using System.Collections.Generic;

namespace IntegrityTables;

internal static class TriggerGuard
{
    private static readonly Stack<Type> Active = new Stack<Type>();
    public static void Enter(Type type)
    {
        if (Active.Contains(type))
            throw new InvalidOperationException($"Trigger cycle detected {type.Name}.");
        Active.Push(type);
    }
    public static void Exit(Type type)
    {
        if (Active.Count == 0 || Active.Peek() != type)
            throw new InvalidOperationException(
                $"TriggerGuard.Exit called for {type.Name}.."
            );
        Active.Pop();
    }
}