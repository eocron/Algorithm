using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class LinearBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly TimeSpan _step;
        private readonly int _maxCount;

        public LinearBackOffIntervalProvider(TimeSpan step, int maxCount = int.MaxValue)
        {
            if (maxCount <= 0)
                throw new ArgumentNullException(nameof(maxCount));
            _step = step;
            _maxCount = maxCount;
        }

        public TimeSpan GetNext(BackOffContext context)
        {
            var n = Math.Min(context.N, _maxCount);
            if (n <= 0)
            {
                return default;
            }
            return n * _step;
        }
    }
}