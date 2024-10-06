using System;

namespace Eocron.Algorithms.Backoff
{
    public sealed class ConstantBackOffIntervalProvider : IBackOffIntervalProvider
    {
        private readonly TimeSpan _value;

        public ConstantBackOffIntervalProvider(TimeSpan value)
        {
            _value = value;
        }
        public TimeSpan GetNext(BackOffContext context)
        {
            return _value;
        }
    }
}