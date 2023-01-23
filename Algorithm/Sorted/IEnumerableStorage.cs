using System.Collections.Generic;

namespace Eocron.Algorithms.Sorted
{
    public interface IEnumerableStorage<TElement>
    {
        void Push(IEnumerable<TElement> data);

        IEnumerable<TElement> Pop();

        int Count { get; }

        void Clear();
    }
}