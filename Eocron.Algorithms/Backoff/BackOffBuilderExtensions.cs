using System;

namespace Eocron.Algorithms.Backoff
{
    public static class BackOffBuilderExtensions
    {
        public static BackOffBuilder WithExponential(this BackOffBuilder builder, TimeSpan initial, float exponent, int maxCount = Int32.MaxValue)
        {
            builder.Provider = new ExponentialBackOffIntervalProvider(initial, exponent, maxCount);
            return builder;
        }
        
        public static BackOffBuilder WithLinear(this BackOffBuilder builder, TimeSpan step, int maxCount = Int32.MaxValue)
        {
            builder.Provider = new LinearBackOffIntervalProvider(step, maxCount);
            return builder;
        }
        
        public static BackOffBuilder WithClamp(this BackOffBuilder builder, TimeSpan min, TimeSpan max)
        {
            builder.Provider = new ClampBackOffIntervalProvider(builder.Provider, min, max);
            return builder;
        }
        
        public static BackOffBuilder WithOffset(this BackOffBuilder builder, TimeSpan offset)
        {
            builder.Provider = new OffsetBackOffIntervalProvider(builder.Provider, offset);
            return builder;
        }
        
        public static BackOffBuilder WithJitter(this BackOffBuilder builder, Random random, TimeSpan jitter)
        {
            builder.Provider = new JitterBackOffIntervalProvider(builder.Provider, random, jitter);
            return builder;
        }
    }
}