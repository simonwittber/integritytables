using System;

namespace IntegrityTables;

public interface IListChangeNotifier<T>
{
    event Action<int, T> ItemAdded;
    event Action<int, T> ItemRemoved;
    event Action<int, T> ItemUpdated;
    event Action Cleared;
}