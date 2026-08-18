namespace Transportation_System.Data;

public class StopQueue
{
    private class Node(int stopId)
    {
        public readonly int StopId = stopId;
        public Node? Next;
    }

    private Node? _head;
    private Node? _tail;

    public int Count { get; private set; }
    public bool IsEmpty => _head == null;

    public StopQueue()
    {
    }

    public StopQueue(IEnumerable<int> stopIds)
    {
        foreach (var id in stopIds) Enqueue(id);
    }

    public int? Peek() => _head?.StopId;

    public int? Dequeue()
    {
        if (_head == null) return null;

        var stopId = _head.StopId;
        _head = _head.Next;
        if (_head == null) _tail = null;
        Count--;
        return stopId;
    }

    public void Enqueue(int stopId)
    {
        var node = new Node(stopId);
        if (_tail == null)
        {
            _head = node;
            _tail = node;
        }
        else
        {
            _tail.Next = node;
            _tail = node;
        }

        Count++;
    }

    public bool Remove(int stopId)
    {
        Node? prev = null;
        var current = _head;
        while (current != null)
        {
            if (current.StopId == stopId)
            {
                if (prev == null) _head = current.Next;
                else prev.Next = current.Next;

                if (current == _tail) _tail = prev;

                Count--;
                return true;
            }

            prev = current;
            current = current.Next;
        }

        return false;
    }

    public void ReplaceAll(IEnumerable<int> newOrder)
    {
        _head = _tail = null;
        Count = 0;
        foreach (var id in newOrder) Enqueue(id);
    }

    public List<int> ToList()
    {
        var result = new List<int>(Count);
        var current = _head;
        while (current != null)
        {
            result.Add(current.StopId);
            current = current.Next;
        }

        return result;
    }
}