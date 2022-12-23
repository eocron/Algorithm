using System.Diagnostics;
using NUnit.Framework;

namespace Eocron.Sharding.Tests
{
    public class StreamTests
    {
        [Test]
        [TestCase(new String[0], new String[0])]
        [TestCase(new[] { "a" }, new[] { "a" })]
        [TestCase(new[] { "a", "b", "c" }, new[] { "a", "b", "c" })]
        public async Task TestInputOutput(string[] inputs, string[] outputs)
        {
            var cts = new CancellationTokenSource(TestTimeout);
            await _shard.PublishAsync(inputs, cts.Token);
            await Task.WhenAll(
                AssertIsEqual(_shard.Outputs.AsAsyncEnumerable(cts.Token), TimeSpan.FromSeconds(1), outputs),
                AssertIsEmpty(_shard.Errors.AsAsyncEnumerable(cts.Token), TimeSpan.FromSeconds(1)));
        }
        [Test]
        [Explicit]
        public async Task StressTest2()
        {
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        [Test]
        [Explicit]
        public async Task StressTest()
        {

            int count = 1000000;
            var data = Enumerable.Range(0, count).Select(x => Guid.NewGuid().ToString()).ToList();

            var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            await Task.WhenAll(
                _shard.PublishAsync(data, cts.Token),
                AssertIsEqual(_shard.Outputs.AsAsyncEnumerable(cts.Token), Timeout.InfiniteTimeSpan));
        }

        private static Task AssertIsEmpty<T>(IAsyncEnumerable<T> enumerable, TimeSpan forTime)
        {
            return AssertIsEqual(enumerable, forTime);
        }
        private static async Task AssertIsEqual<T>(IAsyncEnumerable<T> enumerable, TimeSpan forTime, params T[] expected)
        {
            expected = expected ?? Array.Empty<T>();
            var result = await ConsumeFor(enumerable, forTime).ConfigureAwait(false);
            CollectionAssert.AreEqual(expected, result);
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
            catch (OperationCanceledException)when (cts.Token.IsCancellationRequested)
            {
            }

            return result;
        }
        private static ProcessStartInfo CreateTestAppInfo(string mode)
        {
            return new ProcessStartInfo("Tools/Eocron.Sharding.TestApp.exe") { ArgumentList = { mode } }.ConfigureAsService();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            var logger = new TestLogger();
            _cts = new CancellationTokenSource();
            _shard = new RestartUntilFinishedShard<string, string, string>(
                new ProcessShard<string, string, string>(
                    CreateTestAppInfo("Stream"),
                    new NewLineDeserializer(),
                    new NewLineDeserializer(),
                    new NewLineSerializer(),
                    logger),
                logger,
                TimeSpan.Zero);
            _task = _shard.RunAsync(_cts.Token);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _cts.Cancel();
            await _task.ConfigureAwait(false);
        }

        private CancellationTokenSource _cts;
        private IShard<string, string, string> _shard;
        private Task _task;
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    }
}