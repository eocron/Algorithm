using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Gauge;
using Eocron.Sharding.Jobs;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Monitoring
{
    public class ShardMonitoringJob<TInput> : IJob
    {
        private readonly IShardInputManager<TInput> _inputManager;
        private readonly IProcessDiagnosticInfoProvider _infoProvider;
        private readonly IMetrics _metrics;
        private readonly TimeSpan _checkInterval;

        private readonly GaugeOptions _workingSetGauge;
        private readonly GaugeOptions _cpuPercentageGauge;
        private readonly GaugeOptions _privateMemoryGauge;
        private DateTime? _lastCheckTime;
        private TimeSpan? _lastTotalProcessorTime;

        public ShardMonitoringJob(
            IShardInputManager<TInput> inputManager,
            IProcessDiagnosticInfoProvider infoProvider,
            IMetrics metrics,
            TimeSpan checkInterval,
            IReadOnlyDictionary<string, string> tags)
        {
            _inputManager = inputManager;
            _infoProvider = infoProvider;
            _metrics = metrics;
            _checkInterval = checkInterval;
            _workingSetGauge = MonitoringHelper.CreateShardOptions<GaugeOptions>("working_set_bytes",
                x => { x.MeasurementUnit = Unit.Bytes; }, tags: tags);
            _privateMemoryGauge = MonitoringHelper.CreateShardOptions<GaugeOptions>("private_memory_bytes",
                x => { x.MeasurementUnit = Unit.Bytes; }, tags: tags);
            _cpuPercentageGauge = MonitoringHelper.CreateShardOptions<GaugeOptions>("cpu_load_percents",
                x => { x.MeasurementUnit = Unit.Percent; }, tags: tags);
        }

        public async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                OnCheck();
                await Task.Delay(_checkInterval, ct).ConfigureAwait(false);
            }
        }
        private void OnCheck()
        {
            _inputManager.IsReady();

            if (!_infoProvider.TryGetProcessDiagnosticInfo(out var info))
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

        public void Dispose()
        {
        }
    }
}