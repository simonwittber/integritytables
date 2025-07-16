using System;
using System.Collections.Generic;

namespace IntegrityTables;

internal class HeapQueue<T> where T : IComparable<T>
{
    private readonly List<T> _items;

    public int Count => _items.Count;

    public bool IsEmpty => _items.Count == 0;

    public T First => _items[0];

    public void Clear() => _items.Clear();

    public bool Contains(T item) => _items.Contains(item);

    public void Remove(T item) => _items.Remove(item);

    public T Peek() => _items[0];

    public HeapQueue()
    {
        _items = new List<T>();
    }

    public void Push(T item)
    {
        //add item to end of tree to extend the list
        _items.Add(item);
        //find correct position for new item.
        SiftDown(0, _items.Count - 1);
    }

    public T Pop()
    {

        //if there are more than 1 items, returned item will be first in tree.
        //then, add last item to front of tree, shrink the list
        //and find correct index in tree for first item.
        T item;
        var last = _items[^1];
        _items.RemoveAt(_items.Count - 1);
        if (_items.Count > 0)
        {
            item = _items[0];
            _items[0] = last;
            SiftUp();
        }
        else
        {
            item = last;
        }
        return item;
    }


    private int Compare(T a, T b) => a.CompareTo(b);

    private void SiftDown(int startpos, int pos)
    {
        //preserve the newly added item.
        var newItem = _items[pos];
        while (pos > startpos)
        {
            //find parent index in binary tree
            var parentPos = (pos - 1) >> 1;
            var parent = _items[parentPos];
            //if new item precedes or equal to parent, pos is new item position.
            if (Compare(parent, newItem) <= 0)
                break;
            //else move parent into pos, then repeat for grand parent.
            _items[pos] = parent;
            pos = parentPos;
        }
        _items[pos] = newItem;
    }

    private void SiftUp()
    {
        var endPos = _items.Count;
        var startPos = 0;
        //preserve the inserted item
        var newItem = _items[0];
        var childPos = 1;
        var pos = 0;
        //find child position to insert into binary tree
        while (childPos < endPos)
        {
            //get right branch
            var rightPos = childPos + 1;
            //if right branch should precede left branch, move right branch up the tree
            if (rightPos < endPos && Compare(_items[rightPos], _items[childPos]) <= 0)
                childPos = rightPos;
            //move child up the tree
            _items[pos] = _items[childPos];
            pos = childPos;
            //move down the tree and repeat.
            childPos = 2 * pos + 1;
        }
        //the child position for the new item.
        _items[pos] = newItem;
        SiftDown(startPos, pos);
    }
    
    
}