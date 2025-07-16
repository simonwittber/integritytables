using System;

namespace IntegrityTables;

public static class Warnings
{
    public static event Action<string>? OnWarning;
    public static event Action<string>? OnLog;

    public static void Warn(string msg)
    {
        OnWarning?.Invoke(msg);
    }

    public static void Log(string msg)
    {
        OnLog?.Invoke(msg);
    }

    public static void ClearEventCallbacks()
    {
        OnWarning = null;
        OnLog = null;
    }
}