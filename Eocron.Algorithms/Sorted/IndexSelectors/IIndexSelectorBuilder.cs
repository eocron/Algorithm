using System.Collections.Generic;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    public interface IIndexSelectorBuilder<T>
    {
        IIndexSelector Build(IList<T> collection, T value, IComparer<T> comparer);
    }
}