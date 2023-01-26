using System.Collections.Generic;

namespace Eocron.Algorithms.Sorted
{
    public interface IEnumerableStorage<TElement>
    {
        /// <summary>
        /// Add collection
        /// </summary>
        /// <param name="data"></param>
        void Add(IEnumerable<TElement> data);
        
        /// <summary>
        /// Take any available collection
        /// </summary>
        /// <returns></returns>
        IEnumerable<TElement> Take();
        
        int Count { get; }

        void Clear();
    }
}