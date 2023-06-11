using System;
using System.Collections.Generic;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    /// <summary>
    /// Default binary index selector - just selecting middle of the range.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class LogarithmicIndexSelectorBuilder<T> : IIndexSelectorBuilder<T>, IIndexSelector
    {
        public static readonly LogarithmicIndexSelectorBuilder<T> Default = new LogarithmicIndexSelectorBuilder<T>();

        private LogarithmicIndexSelectorBuilder(){}
        public int GetNextMiddle(Range range)
        {
            return range.Start.Value + ((range.End.Value - range.Start.Value) >> 1);
        }

        public IIndexSelector Build(IList<T> collection, T value, IComparer<T> comparer)
        {
            return this;
        }
    }
}