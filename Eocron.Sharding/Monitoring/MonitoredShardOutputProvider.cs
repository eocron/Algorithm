using System;
using System.Collections.Generic;
using System.Threading.Channels;
using App.Metrics;
using App.Metrics.Histogram;

namespace Eocron.Sharding.Monitoring
{
    public class MonitoredShardOutputProvider<TOutput, TError> : IShardOutputProvider<TOutput, TError>
    {
        private readonly IShardOutputProvider<TOutput, TError> _inner;
        private readonly IMetrics _metrics;
        private readonly HistogramOptions _outputDelayHistogramOptions;
        private readonly HistogramOptions _errorDelayHistogramOptions;

        public MonitoredShardOutputProvider(IShardOutputProvider<TOutput, TError> inner, IMetrics metrics, IReadOnlyDictionary<string, string> tags)
        {
            _inner = inner;
            _metrics = metrics;
            _outputDelayHistogramOptions = MonitoringHelper.CreateShardOptions<HistogramOptions>("output_read_delay_ms", tags: tags);
            _errorDelayHistogramOptions = MonitoringHelper.CreateShardOptions<HistogramOptions>("error_read_delay_ms", tags: tags);
        }

        public ChannelReader<ShardMessage<TOutput>> Outputs => new MonitoredChannelReader<ShardMessage<TOutput>, TOutput>(
            _inner.Outputs,
            _metrics,
            _outputDelayHistogramOptions);

        public ChannelReader<ShardMessage<TError>> Errors => new MonitoredChannelReader<ShardMessage<TError>, TError>(
            _inner.Errors,
            _metrics,
            _errorDelayHistogramOptions);
    }
}