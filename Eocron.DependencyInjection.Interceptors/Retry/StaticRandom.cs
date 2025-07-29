using System;
using System.Threading;

namespace Eocron.DependencyInjection.Interceptors.Retry
{
    public static class StaticRandom
    {
        private static int _seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> Random = new(() => new Random(Interlocked.Increment(ref _seed)));
        public static Random Value => Random.Value;

        public static double NextDouble()
        {
            return Random.Value.NextDouble();
        }
        
        public static int Next(int maxValue)
        {
            return Random.Value.Next(maxValue);
        }
        
        public static int Next(int minValue, int maxValue)
        {
            return Random.Value.Next(minValue, maxValue);
        }
    }
}