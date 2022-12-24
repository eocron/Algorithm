using System;
using App.Metrics;
using App.Metrics.Gauge;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Monitoring
{
    public class AppMetricsProcessShard<TInput, TOutput, TError> : AppMetricsShard<TInput, TOutput, TError>, IProcessShard<TInput, TOutput, TError>
    {
        private readonly IProcessShard<TInput, TOutput, TError> _inner;
        private readonly IMetrics _metrics;
        private readonly GaugeOptions _workingSetGauge;
        private readonly GaugeOptions _cpuPercentageGauge;
        private readonly GaugeOptions _privateMemoryGauge;
        private DateTime? _lastCheckTime;
        private TimeSpan? _lastTotalProcessorTime;

        public AppMetricsProcessShard(IProcessShard<TInput, TOutput, TError> inner, IMetrics metrics, TimeSpan? statusCheckInterval = null) : base(inner, metrics, statusCheckInterval)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _workingSetGauge = CreateOptions<GaugeOptions>("working_set_bytes", x =>
            {
                x.MeasurementUnit = Unit.Bytes;
            });
            _privateMemoryGauge = CreateOptions<GaugeOptions>("private_memory_bytes", x =>
            {
                x.MeasurementUnit = Unit.Bytes;
            });
            _cpuPercentageGauge = CreateOptions<GaugeOptions>("cpu_load_percents", x =>
            {
                x.MeasurementUnit = Unit.Percent;
            });
            OnCheck += AppMetricsProcessShard_OnCheck;
        }

        private void AppMetricsProcessShard_OnCheck(object sender, EventArgs e)
        {
            if (!_inner.TryGetProcessDiagnosticInfo(out var info))
                info = new ProcessDiagnosticInfo
                {
                    PrivateMemorySize64 = 0,
                    TotalProcessorTime = TimeSpan.Zero,
                    WorkingSet64 = 0
                };

            _metrics.Measure.Gauge.SetValue(_workingSetGauge, info.WorkingSet64);
            _metrics.Measure.Gauge.SetValue(_privateMemoryGauge, info.PrivateMemorySize64);
            _metrics.Measure.Gauge.SetValue(_cpuPercentageGauge, SampleCpuUsage(info) * 100);
        }

        private float SampleCpuUsage(ProcessDiagnosticInfo info)
        {
            _lastCheckTime ??= DateTime.UtcNow;
            _lastTotalProcessorTime ??= TimeSpan.Zero;
            var currentTotalProcessorTime = info.TotalProcessorTime;
            var currentCheckTime = DateTime.UtcNow;
            var cpuPercents = GetCpuUsage(_lastTotalProcessorTime.Value, currentTotalProcessorTime, _lastCheckTime.Value, currentCheckTime);

            _lastCheckTime = currentCheckTime;
            _lastTotalProcessorTime = currentTotalProcessorTime;

            return cpuPercents;
        }

        private static float GetCpuUsage(
            TimeSpan startTotalProcessorTime,
            TimeSpan endTotalProcessorTime,
            DateTime startCheckTime,
            DateTime endCheckTime)
        {
            var diffProcessorTime = endTotalProcessorTime.Ticks - startTotalProcessorTime.Ticks;
            var diffElapsedTime = (startCheckTime.Ticks - endCheckTime.Ticks) * Environment.ProcessorCount;

            var res = diffProcessorTime / (float)diffElapsedTime;
            if (float.IsInfinity(res) || float.IsNaN(res))
                return 0;
            if (res > 1)
                return 1;
            if (res < 0)
                return 0;
            return res;
        }

        public bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info)
        {
            return _inner.TryGetProcessDiagnosticInfo(out info);
        }
    }
}