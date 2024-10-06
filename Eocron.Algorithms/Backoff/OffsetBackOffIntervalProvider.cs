using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class OffsetBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly IBackOffIntervalProvider _provider;
        private readonly TimeSpan _offset;

        public OffsetBackOffIntervalProvider(IBackOffIntervalProvider provider, TimeSpan offset)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _offset = offset;
        }

        public TimeSpan GetNext(BackOffContext context)
        {
            return _offset + _provider.GetNext(context);
        }
    }
}