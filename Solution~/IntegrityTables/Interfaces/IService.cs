using System;

namespace IntegrityTables;

public interface IService<T> : IDisposable
{
    protected T db { get; set; }

    public void SetDatabase(T db);

    protected void ConnectTriggers();
    
    protected void DisconnectTriggers();
}