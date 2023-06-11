using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.Queues
{
    /// <summary>
    ///     Priority queue interface. Represents queue in which order of element affect Equeue/Dequeue operations
    ///     in such way, that at any given time Dequeue will return first priority items.
    /// </summary>
    /// <typeparam name="TPriority"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public interface IPriorityQueue<TPriority, TValue>
    {
        void Clear();

        /// <summary>
        ///     Dequeue first priority item.
        ///     Throws error if queue is empty.
        /// </summary>
        /// <returns></returns>
        KeyValuePair<TPriority, TValue> Dequeue();

        /// <summary>
        ///     Enqueue new item.
        /// </summary>
        /// <param name="item"></param>
        void Enqueue(KeyValuePair<TPriority, TValue> item);

        /// <summary>
        ///     Inserts new item in queue, or update item if it is already exist.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="onUpdate"></param>
        void EnqueueOrUpdate(KeyValuePair<TPriority, TValue> item,
            Func<KeyValuePair<TPriority, TValue>, KeyValuePair<TPriority, TValue>> onUpdate);

        /// <summary>
        ///     Peek first priority item.
        ///     Throws error if queue is empty.
        /// </summary>
        /// <returns></returns>
        KeyValuePair<TPriority, TValue> Peek();

        int Count { get; }
    }
}