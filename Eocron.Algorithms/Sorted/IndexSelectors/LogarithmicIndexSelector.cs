using System;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    public sealed class LogarithmicIndexSelector : IIndexSelector
    {
        public int GetNextMiddle(Range range)
        {
            return range.Start.Value + ((range.End.Value - range.Start.Value) >> 1);
        }
    }
}