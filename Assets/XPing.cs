using System;
using System.Collections.Generic;
public class XPing
{
    private Queue<int> _queue = new();
    private int _maxSize;

    public XPing(int maxSize = 100)
    {
        _maxSize = maxSize;
    }

    public void Add(double item)
    {
        Add(Convert.ToInt32(item));
    }
    
    public void Add(int item)
    {
        _queue.Enqueue(item);

        if (_queue.Count > _maxSize)
        {
            _queue.Dequeue();
        }
    }

    public int Latest => _queue.TryPeek(out var ping) ? ping : 0;

    public int Avg
    {
        get
        {
            if (_queue.Count == 0)
            {
                return 0;
            }
            double sum = 0;
            int count=0;
            foreach (var ping in _queue)
            {
                sum += ping;
                count++;
            }

            return Convert.ToInt32(sum / count);
        }
    }

    public override string ToString() => Avg.ToString();
}
