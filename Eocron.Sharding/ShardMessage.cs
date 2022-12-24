using System;

namespace Eocron.Sharding
{
    public class ShardMessage<T>
    {
        public DateTime Timestamp { get; set; }
        public T Value { get; set; }
    }
}