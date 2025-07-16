using UnityEngine;
using UnityEngine.Networking.PlayerConnection;


namespace IntegrityTables
{
    public abstract class ScriptableDatabase : ScriptableObject
    {
        [SerializeReference] public IDatabase database;
    }
    
    public abstract class ScriptableDatabase<T> : ScriptableDatabase where T : class, IDatabase, new()
    {
        void OnEnable()
        {
            database = new T();
        }
    }
    
}