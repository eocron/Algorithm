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
                AssertIsEqual(_shard.GetOutputEnumerable(cts.Token), TimeSpan.FromSeconds(1), outputs),
                AssertIsEmpty(_shard.GetErrorsEnumerable(cts.Token), TimeSpan.FromSeconds(1)));
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
            _shard = new AutoRestartingShard<string, string, string>(
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