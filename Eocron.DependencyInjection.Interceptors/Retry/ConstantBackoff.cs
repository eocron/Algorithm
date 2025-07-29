using System;

namespace Eocron.DependencyInjection.Interceptors.Retry
{
    public static class ConstantBackoff
    {
        public static TimeSpan Calculate(Random random, TimeSpan interval, bool jittered)
        {
            if(interval <= TimeSpan.Zero)
                return TimeSpan.Zero;
            if (!jittered) 
                return interval;
            var stepMs = random.Next((int)interval.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(stepMs);
        }
    }
}