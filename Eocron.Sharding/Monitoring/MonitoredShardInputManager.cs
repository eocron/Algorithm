using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Histogram;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Monitoring
{
    public class MonitoredShardInputManager<TInput> : IShardInputManager<TInput>
    {
        public MonitoredShardInputManager(IShardInputManager<TInput> inner, IMetrics metrics,
            IReadOnlyDictionary<string, string> tags)
        {
            _inner = inner;
            _metrics = metrics;
            _errorCounterOptions = MonitoringHelper.CreateShardOptions<CounterOptions>("error_count", tags: tags);
            _publishDelayOptions =
                MonitoringHelper.CreateShardOptions<HistogramOptions>("input_write_delay_ms", tags: tags);
            _readyForPublishOptions = MonitoringHelper.CreateShardOptions<GaugeOptions>("is_ready", tags: tags);
        }

        public async Task<bool> IsReadyAsync(CancellationToken ct)
        {
            try
            {
                var tmp = await _inner.IsReadyAsync(ct).ConfigureAwait(false);
                _metrics.Measure.Gauge.SetValue(_readyForPublishOptions, tmp ? 1 : 0);
                return tmp;
            }
            catch
            {
                _metrics.Measure.Counter.Increment(_errorCounterOptions);
                throw;
            }
        }

        public async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            var count = 0;
            var sw = Stopwatch.StartNew();
            try
            {
                await _inner.PublishAsync(messages?.Select(x =>
                {
                    Interlocked.Increment(ref count);
                    return x;
                }), ct).ConfigureAwait(false);
            }
            catch
            {
                _metrics.Measure.Counter.Increment(_errorCounterOptions);
                throw;
            }
            finally
            {
                _metrics.Measure.Histogram.Update(_publishDelayOptions,
                    sw.ElapsedTicks / TimeSpan.TicksPerMillisecond);
            }
        }

        private readonly CounterOptions _errorCounterOptions;
        private readonly GaugeOptions _readyForPublishOptions;
        private readonly HistogramOptions _publishDelayOptions;
        private readonly IMetrics _metrics;
        private readonly IShardInputManager<TInput> _inner;
    }
}