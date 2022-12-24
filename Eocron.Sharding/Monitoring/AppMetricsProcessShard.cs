using System;
using System.Threading;
using System.Threading.Tasks;
using App.Metrics;
using App.Metrics.Gauge;
using Eocron.Sharding.Processing;

namespace Eocron.Sharding.Monitoring
{
    public class AppMetricsProcessShard<TInput, TOutput, TError> : AppMetricsShard<TInput, TOutput, TError>
    {
        private readonly IProcessShard<TInput, TOutput, TError> _inner;
        private readonly IMetrics _metrics;
        private Task _monitorTask;
        private readonly CancellationTokenSource _cts;
        private readonly TimeSpan _interval;
        private readonly GaugeOptions _workingSetGauge;
        private readonly GaugeOptions _totalProcessorTime;

        public AppMetricsProcessShard(IProcessShard<TInput, TOutput, TError> inner, IMetrics metrics, TimeSpan? statusCheckInterval) : base(inner, metrics)
        {
            _inner = inner;
            _metrics = metrics;
            _interval = statusCheckInterval ?? TimeSpan.FromSeconds(1);
            _workingSetGauge = CreateOptions<GaugeOptions>("working_set_bytes", x =>
            {
                x.MeasurementUnit = Unit.Bytes;
            });
            _totalProcessorTime = CreateOptions<GaugeOptions>("total_processor_time_ticks");
            _cts = new CancellationTokenSource();
        }

        public override Task RunAsync(CancellationToken ct)
        {
            _monitorTask = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        _metrics.Measure.Gauge.SetValue(_workingSetGauge, _inner.WorkingSet64 ?? 0);
                        _metrics.Measure.Gauge.SetValue(_totalProcessorTime, (_inner.TotalProcessorTime ?? TimeSpan.Zero).Ticks);
                        await Task.Delay(_interval, _cts.Token);
                    }
                    catch
                    {

                    }
                }
            });
            return base.RunAsync(ct);
        }

        public override void Dispose()
        {
            _cts.Cancel();
            _monitorTask?.Wait();
            _cts.Dispose();
            base.Dispose();
        }
    }
}