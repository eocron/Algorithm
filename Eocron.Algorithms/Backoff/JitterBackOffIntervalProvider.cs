using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class JitterBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly IBackOffIntervalProvider _provider;
        private readonly Random _random;
        private readonly TimeSpan _jitterInterval;
        private readonly object _sync = new();

        public JitterBackOffIntervalProvider(IBackOffIntervalProvider provider, Random random, TimeSpan jitterInterval)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _random = random;
            _jitterInterval = jitterInterval;
        }

        public TimeSpan GetNext(BackOffContext context)
        {
            var jitter = TimeSpan.FromMilliseconds((long)(GetRandomJitter() * _jitterInterval.TotalMilliseconds));
            return _provider.GetNext(context) + jitter;
        }

        private float GetRandomJitter()
        {
            lock (_sync)
            {
                return (float)(_random.NextDouble() - 0.5d);
            }
        }
    }
}