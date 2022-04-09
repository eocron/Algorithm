using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.Queues
{
    public interface IPriorityQueue<TPriority, TValue>
    {
        void Enqueue(KeyValuePair<TPriority, TValue> item);

        void EnqueueOrUpdate(KeyValuePair<TPriority, TValue> item, Func<KeyValuePair<TPriority, TValue>, KeyValuePair<TPriority, TValue>> onUpdate);

        KeyValuePair<TPriority, TValue> Dequeue();

        KeyValuePair<TPriority, TValue> Peek();

        void Clear();

        int Count { get; }
    }
}