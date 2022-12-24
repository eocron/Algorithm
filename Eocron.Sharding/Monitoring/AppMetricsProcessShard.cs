using System;
using App.Metrics;
using App.Metrics.Gauge;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Monitoring
{
    public class AppMetricsProcessShard<TInput, TOutput, TError> : AppMetricsShard<TInput, TOutput, TError>
    {
        private readonly IProcessShard<TInput, TOutput, TError> _inner;
        private readonly IMetrics _metrics;
        private readonly GaugeOptions _workingSetGauge;
        private readonly GaugeOptions _totalProcessorTime;

        public AppMetricsProcessShard(IProcessShard<TInput, TOutput, TError> inner, IMetrics metrics, TimeSpan? statusCheckInterval = null) : base(inner, metrics, statusCheckInterval)
        {
            _inner = inner;
            _metrics = metrics;
            _workingSetGauge = CreateOptions<GaugeOptions>("working_set_bytes", x =>
            {
                x.MeasurementUnit = Unit.Bytes;
            });
            _totalProcessorTime = CreateOptions<GaugeOptions>("total_processor_time_ms");
            OnCheck += AppMetricsProcessShard_OnCheck;
        }

        private void AppMetricsProcessShard_OnCheck(object sender, EventArgs e)
        {
            _metrics.Measure.Gauge.SetValue(_workingSetGauge, _inner.WorkingSet64 ?? 0);
            _metrics.Measure.Gauge.SetValue(_totalProcessorTime, (_inner.TotalProcessorTime ?? TimeSpan.Zero).TotalMilliseconds);
        }
    }
}