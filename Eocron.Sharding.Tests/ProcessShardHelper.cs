using NUnit.Framework;
using System.Diagnostics;
using Eocron.Sharding.Processing;
using App.Metrics;
using Eocron.Sharding.Configuration;
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
            };
        }

        public static IShard<string, string, string> CreateTestShard(string mode)
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(new TestLogger());
            var killer = new TestChildProcessWatcher();
            var metrics = new MetricsBuilder().Build();
            var shardFactory =
                new ShardBuilder<string, string, string>()
                    .WithLogging(loggerFactory.Object)
                    .WithProcessJob((x, id) =>
                            new ProcessJob<string, string, string>(
                                CreateTestAppShardOptions(mode),
                                new NewLineDeserializer(),
                                new NewLineDeserializer(),
                                new NewLineSerializer(),
                                x.GetRequiredService<ILogger>(),
                                id: id,
                                watcher: killer,
                                stateProvider: x.GetService<IProcessStateProvider>()),
                        TimeSpan.Zero,
                        TimeSpan.Zero)
                    .WithAppMetrics(
                        metrics,
                        null,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.Zero)
                    .CreateFactory();
            return shardFactory.CreateNewShard(nameof(ProcessShardTests) + Guid.NewGuid());
        }
    }
}
