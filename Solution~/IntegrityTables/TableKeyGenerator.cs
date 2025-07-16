namespace IntegrityTables;


public class TableKeyGenerator
{
    // protects both _nextId and all operations on it
    private static readonly object Sync = new object();
    private int _nextId;

    public int CurrentKey => _nextId;

    /// <summary>
    /// Make sure that all future IDs are strictly greater than or equal to 'minimum'.
    /// </summary>
    public void EnsureAtLeast(int minimum)
    {
        lock (Sync)
        {
            if (_nextId < minimum)
                _nextId = minimum;
        }
    }

    /// <summary>
    /// Returns the next unique ID (always increasing).
    /// </summary>
    public int NextId()
    {
        lock (Sync)
        {
            _nextId++;
            return _nextId;
        }
    }

    public void Reset(int maxKey)
    {
        lock (Sync) _nextId = maxKey;
    }
}