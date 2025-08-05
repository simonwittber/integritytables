using System;
using System.Collections.Generic;
using System.Threading;

namespace IntegrityTables;

internal static class TriggerGuard
{
    [ThreadStatic] private static Stack<Type> Active;
    public static void Enter(Type type)
    {
        if(Active == null)  Active = new Stack<Type>();
        if (Active.Contains(type))
            throw new InvalidOperationException($"Trigger cycle detected {type.Name}.");
        Active.Push(type);
    }
    public static void Exit(Type type)
    {
        if(Active == null)  Active = new Stack<Type>();
        if (Active.Count == 0 || Active.Peek() != type)
            throw new InvalidOperationException(
                $"TriggerGuard.Exit called for {type.Name}.."
            );
        Active.Pop();
    }
}



