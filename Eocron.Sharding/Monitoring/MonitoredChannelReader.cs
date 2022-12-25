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
        private readonly ChannelReader<TMessage> _inner;
        private readonly IMetrics _metrics;
        private readonly HistogramOptions _messageDelayOptions;
        public override Task Completion => _inner.Completion;
        public override bool CanCount => _inner.CanCount;
        public override bool CanPeek => _inner.CanPeek;
        public override int Count => _inner.Count;

        public MonitoredChannelReader(ChannelReader<TMessage> inner, IMetrics metrics, HistogramOptions messageDelayOptions)
        {
            _inner = inner;
            _metrics = metrics;
            _messageDelayOptions = messageDelayOptions;
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

        public override bool TryPeek(out TMessage item)
        {
            return _inner.TryPeek(out item);
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return _inner.WaitToReadAsync(cancellationToken);
        }

        public override async ValueTask<TMessage> ReadAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await _inner.ReadAsync(cancellationToken).ConfigureAwait(false);
            RecordMetrics(result);
            return result;
        }

        private void RecordMetrics(TMessage msg)
        {
            var delay = DateTime.UtcNow - msg.Timestamp;
            _metrics.Measure.Histogram.Update(_messageDelayOptions, delay.Ticks / TimeSpan.TicksPerMillisecond);
        }
    }
}