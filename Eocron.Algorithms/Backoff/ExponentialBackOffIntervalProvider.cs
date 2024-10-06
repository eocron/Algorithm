using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class ExponentialBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly TimeSpan _initial;
        private readonly float _exponent;
        private readonly int _maxCount;

        public ExponentialBackOffIntervalProvider(TimeSpan initial, float exponent, int maxCount = int.MaxValue)
        {
            if (exponent <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exponent));
            }

            if (maxCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCount));
            }
            _initial = initial;
            _exponent = exponent;
            _maxCount = maxCount;
        }
        public TimeSpan GetNext(BackOffContext context)
        {
            var n = Math.Min(context.N, _maxCount) - 1;
            if (n < 0)
            {
                return default;
            }
            return TimeSpan.FromTicks((long)(_initial.Ticks * Math.Pow(_exponent, n)));
        }
    }
}