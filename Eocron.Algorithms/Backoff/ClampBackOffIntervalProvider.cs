using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class ClampBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly TimeSpan _min;
        private readonly TimeSpan _max;
        private readonly IBackOffIntervalProvider _provider;

        public ClampBackOffIntervalProvider(IBackOffIntervalProvider provider, TimeSpan min, TimeSpan max)
        {
            if (min >= max)
            {
                throw new ArgumentOutOfRangeException($"Invalid clamp interval from {min} to {max}");
            }

            _min = min;
            _max = max;
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public TimeSpan GetNext(BackOffContext context)
        {
            var value = _provider.GetNext(context);
            return TimeSpan.FromTicks(Math.Max(Math.Min(value.Ticks, _max.Ticks), _min.Ticks));
        }
    }
}