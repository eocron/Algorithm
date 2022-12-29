using NUnit.Framework;
using System.Diagnostics;
using System.Threading.Channels;
using Eocron.Sharding.Processing;
using App.Metrics;
using Eocron.Sharding.Configuration;
using Eocron.Sharding.Monitoring;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Eocron.Sharding.Tests
{
    public static class ProcessShardHelper
    {
        public static Task AssertErrorsAndOutputs<TInput, TOutput, TError>(
            IShard<TInput, TOutput, TError> shard,
            TOutput[] outputs, TError[] errors,
            CancellationToken ct,
            TimeSpan forTime)
        {
            return Task.WhenAll(
                AssertIsEqual(shard.Outputs.AsAsyncEnumerable(ct), forTime, outputs),
                AssertIsEqual(shard.Errors.AsAsyncEnumerable(ct), forTime, errors));
        }
        public static Task AssertIsEmpty<T>(IAsyncEnumerable<ShardMessage<T>> enumerable, TimeSpan forTime)
        {
            return AssertIsEqual(enumerable, forTime);
        }
        public static async Task AssertIsEqual<T>(IAsyncEnumerable<ShardMessage<T>> enumerable, TimeSpan forTime, params T[] expected)
        {
            expected = expected ?? Array.Empty<T>();
            var result = await ConsumeFor(enumerable, forTime).ConfigureAwait(false);
            CollectionAssert.AreEqual(expected, result.Select(x=> x.Value));
        }
        private static async Task<List<T>> ConsumeFor<T>(IAsyncEnumerable<T> enumerable, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var result = new List<T>();
            try
            {
                await foreach (var i in enumerable.WithCancellation(cts.Token))
                {
                    result.Add(i);
                }
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
            }

            return result;
        }
        public static ProcessShardOptions CreateTestAppShardOptions(string mode)
        {
            return new ProcessShardOptions()
            {
                StartInfo = new ProcessStartInfo("Tools/Eocron.Sharding.TestApp.exe") { ArgumentList = { mode } }
                    .ConfigureAsService(),
                ErrorRestartInterval = TimeSpan.Zero,
                SuccessRestartInterval = TimeSpan.Zero
            };
        }

        public static IShard<string, string, string> CreateTestShard(string mode, ITestProcessJobHandle handle = null)
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new TestLogger());
            var watcher = new TestChildProcessWatcher();
            var metrics = new MetricsBuilder().Build();
            var shardFactory =
                new ShardBuilder<string, string, string>()
                    .WithTransient<ILoggerFactory>(loggerFactory.Object)
                    .WithTransient<IChildProcessWatcher>(watcher)
                    .WithSerializers(
                        new NewLineSerializer(),
                        new NewLineDeserializer(),
                        new NewLineDeserializer())
                    .WithProcessJob(
                        CreateTestAppShardOptions(mode))
                    .WithProcessJobWrap(x=> new TestProcessJob<string, string, string>(x, handle))
                    .WithTransient<IMetrics>(metrics)
                    .WithAppMetrics(new AppMetricsShardOptions())
                    .CreateFactory();
            return shardFactory.CreateNewShard(nameof(ProcessShardTests) + Guid.NewGuid());
        }

        public interface ITestProcessJobHandle
        {
            void OnStarting();

            void OnStopped();
        }
        public class TestProcessJob<TInput, TOutput, TError> : IProcessJob<TInput, TOutput, TError>
        {
            private readonly IProcessJob<TInput, TOutput, TError> _processJobImplementation;
            private readonly ITestProcessJobHandle _handle;

            public TestProcessJob(IProcessJob<TInput, TOutput, TError> processJobImplementation, ITestProcessJobHandle handle)
            {
                _processJobImplementation = processJobImplementation;
                _handle = handle;
            }

            public string Id => _processJobImplementation.Id;

            public ChannelReader<ShardMessage<TError>> Errors => _processJobImplementation.Errors;

            public ChannelReader<ShardMessage<TOutput>> Outputs => _processJobImplementation.Outputs;

            public Task<bool> IsReadyAsync(CancellationToken ct)
            {
                return _processJobImplementation.IsReadyAsync(ct);
            }

            public Task PublishAsync(IEnumerable<TInput> messages, CancellationToken ct)
            {
                return _processJobImplementation.PublishAsync(messages, ct);
            }

            public bool TryGetProcessDiagnosticInfo(out ProcessDiagnosticInfo info)
            {
                return _processJobImplementation.TryGetProcessDiagnosticInfo(out info);
            }

            public void Dispose()
            {
                _processJobImplementation.Dispose();
            }

            public async Task RunAsync(CancellationToken ct)
            {
                _handle?.OnStarting();
                await _processJobImplementation.RunAsync(ct).ConfigureAwait(false);
                _handle?.OnStopped();
            }
        }
    }
}
