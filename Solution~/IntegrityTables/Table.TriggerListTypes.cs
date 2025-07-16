using System;

namespace IntegrityTables;

public partial class Table<T> where T : struct, IEquatable<T>
{
    
    public class BeforeTriggerList
    {
        private readonly InsertionSortedList<int, ModifyRowDelegate> _items = new();
    
        public static BeforeTriggerList operator +(BeforeTriggerList e, (int priority, ModifyRowDelegate action) item)
        {
            e._items.Add(item.priority, item.action );
            return e;
        }
        
        public static BeforeTriggerList operator +(BeforeTriggerList e, ModifyRowDelegate action)
        {
            e._items.Add(int.MaxValue, action);
            return e;
        }
    
        public static BeforeTriggerList operator -(BeforeTriggerList e, ModifyRowDelegate action)
        {
            e._items.RemoveByValue(action);
            return e;
        }
    
        public void Invoke(ref Row<T> row, bool enableUserTriggers = true)
        {
            foreach(var (priority, item) in _items)
            {
                if(priority > 0 && !enableUserTriggers)
                    continue; // Skip user triggers if not enabled
                item.Invoke(ref row);
            }
        }
    }
    
    public class AfterTriggerList
    {
        private readonly InsertionSortedList<int, InspectRowDelegate> _items = new();
    
        public static AfterTriggerList operator +(AfterTriggerList e, (int priority, InspectRowDelegate action) item)
        {
            e._items.Add(item.priority, item.action );
            return e;
        }
        
        public static AfterTriggerList operator +(AfterTriggerList e, InspectRowDelegate action)
        {
            e._items.Add(int.MaxValue, action);
            return e;
        }
    
        public static AfterTriggerList operator -(AfterTriggerList e, InspectRowDelegate action)
        {
            e._items.RemoveByValue(action);
            return e;
        }
    
        public void Invoke(in Row<T> row, bool enableUserTriggers = true)
        {
            foreach(var (priority, item) in _items)
            {
                if(priority > 0 && !enableUserTriggers)
                    continue; // Skip user triggers if not enabled
                item.Invoke(in row);
            }
        }
    }
    
    public class AfterUpdateTriggerList
    {
        private readonly InsertionSortedList<int, AfterUpdateDelegate> _items = new();
    
        public static AfterUpdateTriggerList operator +(AfterUpdateTriggerList e, (int priority, AfterUpdateDelegate action) item)
        {
            e._items.Add(item.priority, item.action );
            return e;
        }
        
        public static AfterUpdateTriggerList operator +(AfterUpdateTriggerList e, AfterUpdateDelegate action)
        {
            e._items.Add(int.MaxValue, action);
            return e;
        }
    
        public static AfterUpdateTriggerList operator -(AfterUpdateTriggerList e, AfterUpdateDelegate action)
        {
            e._items.RemoveByValue(action);
            return e;
        }
    
        public void Invoke(in Row<T> oldRow, in Row<T> newRow, bool enableUserTriggers = true)
        {
            foreach(var (priority, item) in _items)
            {
                if(priority > 0 && !enableUserTriggers)
                    continue; // Skip user triggers if not enabled
                item.Invoke(in oldRow, in newRow);
            }
        }
    }
    
    public class BeforeUpdateTriggerList
    {
        private readonly InsertionSortedList<int, BeforeUpdateDelegate> _items = new();
    
        public static BeforeUpdateTriggerList operator +(BeforeUpdateTriggerList e, (int priority, BeforeUpdateDelegate action) item)
        {
            e._items.Add(item.priority, item.action );
            return e;
        }
        
        public static BeforeUpdateTriggerList operator +(BeforeUpdateTriggerList e, BeforeUpdateDelegate action)
        {
            e._items.Add(int.MaxValue, action);
            return e;
        }
    
        public static BeforeUpdateTriggerList operator -(BeforeUpdateTriggerList e, BeforeUpdateDelegate action)
        {
            e._items.RemoveByValue(action);
            return e;
        }
    
        public void Invoke(in Row<T> oldRow, ref Row<T> newRow, bool enableUserTriggers = true)
        {
            foreach(var (priority, item) in _items)
            {
                if(priority > 0 && !enableUserTriggers)
                    continue; // Skip user triggers if not enabled
                item.Invoke(in oldRow, ref newRow);
            }
        }
    }
}