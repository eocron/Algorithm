using System;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    public sealed class DepthIndexSelector : IIndexSelector
    {
        private readonly Func<int, IIndexSelector> _getOrCreateIndexSelector;
        private int _depth;

        public DepthIndexSelector(Func<int, IIndexSelector> getOrCreateIndexSelector)
        {
            _getOrCreateIndexSelector = getOrCreateIndexSelector ?? throw new ArgumentNullException(nameof(getOrCreateIndexSelector));
        }
        public int GetNextMiddle(Range range)
        {
            try
            {
                return _getOrCreateIndexSelector(_depth).GetNextMiddle(range);
            }
            finally
            {
                _depth++;
            }
        }
    }
}