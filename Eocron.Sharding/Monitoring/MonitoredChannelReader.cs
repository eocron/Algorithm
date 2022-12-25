using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Histogram;

namespace Eocron.Sharding.Monitoring
{
    public class MonitoredChannelReader<TMessage, TValue> : ChannelReader<TMessage>
        where TMessage : ShardMessage<TValue>
    {
        public MonitoredChannelReader(ChannelReader<TMessage> inner, IMetrics metrics,
            HistogramOptions messageDelayOptions)
        {
            _inner = inner;
            _metrics = metrics;
            _messageDelayOptions = messageDelayOptions;
        }

        public override async ValueTask<TMessage> ReadAsync(CancellationToken cancellationToken = new())
        {
            var result = await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
            RecordMetrics(result);
            return result;
        }

        public override bool TryPeek(out TMessage item)
        {
            return _inner.TryPeek(out item);
        }

        public override bool TryRead(out TMessage item)
        {
            if (_inner.TryRead(out item))
            {
                RecordMetrics(item);
                return true;
            }

            return false;
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new())
        {
            return _inner.WaitToReadAsync(cancellationToken);
        }

        private void RecordMetrics(TMessage msg)
        {
            var delay = DateTime.UtcNow - msg.Timestamp;
            _metrics.Measure.Histogram.Update(_messageDelayOptions, delay.Ticks / TimeSpan.TicksPerMillisecond);
        }

        public override bool CanCount => _inner.CanCount;
        public override bool CanPeek => _inner.CanPeek;
        public override int Count => _inner.Count;
        public override Task Completion => _inner.Completion;
        private readonly ChannelReader<TMessage> _inner;
        private readonly HistogramOptions _messageDelayOptions;
        private readonly IMetrics _metrics;
    }
}