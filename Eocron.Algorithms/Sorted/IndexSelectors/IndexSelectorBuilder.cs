using System.Collections.Generic;
// ReSharper disable StaticMemberInGenericType

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    /// <summary>
    /// Default binary index selector - just selecting middle of the range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class IndexSelectorBuilder<T> : IIndexSelectorBuilder<T>
    {
        public static readonly IndexSelectorBuilder<T> Default = new IndexSelectorBuilder<T>();

        private static readonly IIndexSelector DefaultIndexSelector = new LogarithmicIndexSelector();
        
        public IIndexSelector Build(IList<T> collection, T value, IComparer<T> comparer)
        {
            return DefaultIndexSelector;
        }
    }
}