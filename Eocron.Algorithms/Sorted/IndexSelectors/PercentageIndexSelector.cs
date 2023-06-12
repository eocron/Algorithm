using System;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    public sealed class PercentageIndexSelector : IIndexSelector
    {
        private readonly float _percentage;

        public PercentageIndexSelector(float percentage)
        {
            if (percentage <= 0 || percentage >= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(percentage), "Percentage should be in (0,1) range.");
            }

            _percentage = percentage;
        }
        public int GetNextMiddle(Range range)
        {
            return range.Start.Value + (int)Math.Floor((range.End.Value - range.Start.Value) * _percentage);
        }
    }
}