using System;

namespace Eocron.DependencyInjection.Interceptors.Retry
{
    public static class CorrelatedExponentialBackoff
    {
        public static TimeSpan Calculate(Random random, int attempt, TimeSpan propagationDuration,
            TimeSpan maxPropagationDuration, bool jittered)
        {
            if(attempt < 0)
                throw new ArgumentOutOfRangeException(nameof(attempt));
            if(propagationDuration >= maxPropagationDuration)
                throw new ArgumentOutOfRangeException(nameof(propagationDuration), "Minimum propagation duration must be less than max propagation duration.");
            if (maxPropagationDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxPropagationDuration), "Maximum propagation duration must be greater than zero.");
            
            var minPropagationMs = Math.Max((int)propagationDuration.TotalMilliseconds, 5); //min time it takes to process single request
            var maxPropagationMs = Math.Max(minPropagationMs, (int)maxPropagationDuration.TotalMilliseconds); //max time it takes to process single request
            var maxAttemptExp = (int)Math.Floor(Math.Log2((maxPropagationMs - minPropagationMs) / minPropagationMs));
            if (maxAttemptExp >= attempt)
            {
                var duration = minPropagationMs * (1 << attempt);
                return TimeSpan.FromMilliseconds(jittered ? random.Next(duration) : duration);
            }
            else
            {
                return ConstantBackoff.Calculate(random, maxPropagationDuration, jittered);
            }
        }
    }
}