using System;

namespace Eocron.Algorithms.Backoff
{
    public static class BackOffBuilderExtensions
    {
        public static BackOffBuilder WithExponential(this BackOffBuilder builder, TimeSpan initial, float exponent, int maxCount = Int32.MaxValue)
        {
            builder._provider = new ExponentialBackOffIntervalProvider(initial, exponent, maxCount);
            return builder;
        }
        
        public static BackOffBuilder WithLinear(this BackOffBuilder builder, TimeSpan step, int maxCount = Int32.MaxValue)
        {
            builder._provider = new LinearBackOffIntervalProvider(step, maxCount);
            return builder;
        }
        
        public static BackOffBuilder WithClamp(this BackOffBuilder builder, TimeSpan min, TimeSpan max)
        {
            builder._provider = new ClampBackOffIntervalProvider(min, max, builder._provider);
            return builder;
        }
    }
}