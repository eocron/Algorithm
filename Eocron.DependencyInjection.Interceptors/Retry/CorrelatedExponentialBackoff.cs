using System;

namespace Eocron.DependencyInjection.Interceptors.Retry
{
    public static class CorrelatedExponentialBackoff
    {
        /// <summary>
        /// Calculates timeframes in which multiple clients can invoke requests at single channel and scatters them in this timeframe.
        /// Formula: timeToWait = random(0, min(minPropagation * 2^(attempt-1), maxPropagation))
        /// </summary>
        /// <param name="minPropagationDuration">Minimum time it takes to complete request, not less than 1ms</param>
        /// <param name="maxPropagationDuration">Maximum time it takes to complete request</param>
        /// <param name="jittered">Enables jittering to avoid clustering of invocations between different systems. Disable to make things worse.</param>
        /// <returns>Time to wait</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static TimeSpan Calculate(Random random, int attempt, TimeSpan minPropagationDuration,
            TimeSpan maxPropagationDuration, bool jittered)
        {
            if(attempt < 1)
                throw new ArgumentOutOfRangeException(nameof(attempt));
            if(minPropagationDuration >= maxPropagationDuration)
                throw new ArgumentOutOfRangeException(nameof(minPropagationDuration), "Minimum propagation duration must be less than maximum propagation duration.");
            if (maxPropagationDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(maxPropagationDuration), "Maximum propagation duration must be greater than zero.");

            var power = attempt - 1;
            var minPropagationMs = Math.Max((int)minPropagationDuration.TotalMilliseconds, 20); //min time it takes to process single request
            var maxPropagationMs = Math.Max(minPropagationMs<<1, (int)maxPropagationDuration.TotalMilliseconds); //max time it takes to process single request
            var maxPower = (int)Math.Floor(Math.Log2((maxPropagationMs - minPropagationMs) / minPropagationMs));
            if (maxPower >= power)
            {
                var duration = minPropagationMs * (1 << power);
                return TimeSpan.FromMilliseconds(jittered ? random.Next(duration) : duration);
            }
            else
            {
                return ConstantBackoff.Calculate(random, maxPropagationDuration, jittered);
            }
        }
    }
}