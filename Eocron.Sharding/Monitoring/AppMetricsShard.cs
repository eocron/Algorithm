using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Counter;
using App.Metrics.Gauge;
using App.Metrics.Timer;

namespace Eocron.Sharding.Monitoring
{
    public class AppMetricsShard<TInput, TOutput, TError> : IShard<TInput, TOutput, TError>
    {
        private readonly IShard<TInput, TOutput, TError> _inner;
        private readonly IMetrics _metrics;
        private readonly TimerOptions _publishTimeOptions;
        private readonly CounterOptions _publishCounterOptions;
        private readonly GaugeOptions _readyForPublishOptions;
        private readonly CounterOptions _errorCounterOptions;
        private readonly CounterOptions _runCounterOptions;

        public AppMetricsShard(IShard<TInput, TOutput, TError> inner, IMetrics metrics)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _errorCounterOptions = CreateOptions<CounterOptions>("error_count");
            _runCounterOptions = CreateOptions<CounterOptions>("run_count");
            _publishCounterOptions = CreateOptions<CounterOptions>("publish_message_count");
            _publishTimeOptions = CreateOptions<TimerOptions>("publish_time", x =>
            {
                x.DurationUnit = TimeUnit.Milliseconds;
                x.RateUnit = TimeUnit.Milliseconds;
            });
            _readyForPublishOptions = CreateOptions<GaugeOptions>("is_ready");
        }

        protected T CreateOptions<T>(string name, Action<T> configure = null) where T : MetricValueOptionsBase, new()
        {
            var result = new T
            {
                Context = "shard",
                Name = name,
                MeasurementUnit = Unit.Events,
                Tags = MetricTags.Concat(new MetricTags(), new Dictionary<string, string>
                {
                    { "input_type", typeof(TInput).Name.ToLowerInvariant() },
                    { "output_type", typeof(TOutput).Name.ToLowerInvariant() },
                    { "error_type", typeof(TError).Name.ToLowerInvariant() },
                    { "id", _inner.Id }
                })
            };
            configure?.Invoke(result);
            return result;
        }

        public string Id => _inner.Id;
        public ChannelReader<ShardMessage<TOutput>> Outputs => _inner.Outputs;
        public ChannelReader<ShardMessage<TError>> Errors => _inner.Errors;

        public virtual bool IsReadyForPublish()
        {
            try
            {
                var result = _inner.IsReadyForPublish();
                _metrics.Measure.Gauge.SetValue(_readyForPublishOptions, result ? 1 : 0);
                return result;
            }
            catch
            {
                _metrics.Measure.Gauge.SetValue(_readyForPublishOptions, 0);
                _metrics.Measure.Counter.Increment(_errorCounterOptions);
                throw;
            }
        }

        public virtual async Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
        {
            int count = 0;
            using (_metrics.Measure.Timer.Time(_publishTimeOptions))
            {
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
                    _metrics.Measure.Counter.Increment(_publishCounterOptions, count);
                }
            }
        }

        public virtual async Task RunAsync(CancellationToken ct)
        {
            try
            {
                await _inner.RunAsync(ct).ConfigureAwait(false);
            }
            catch
            {
                _metrics.Measure.Counter.Increment(_errorCounterOptions);
                throw;
            }
            finally
            {
                _metrics.Measure.Counter.Increment(_runCounterOptions);
            }
        }

        public virtual void Dispose()
        {
            _inner.Dispose();
        }
    }
}
