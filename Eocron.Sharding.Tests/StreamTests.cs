using NUnit.Framework;

namespace Eocron.Sharding.Tests
{
    public class StreamTests
    {
        private static IEnumerable<TestCaseData> GetTestCases()
        {
            yield return new TestCaseData(new[] { Array.Empty<string>() }, Array.Empty<string>()).SetName("NoInput");
            yield return new TestCaseData(new[] { new[] { "a" } }, new[] { "a" }).SetName("OneInOneOut");
            yield return new TestCaseData(new[] { new[] { "a", "stop" }, new[] { "c" } }, new[] { "a", "stop", "c" }).SetName("RestartInMiddle");
            yield return new TestCaseData(new[] { new[] { "a", "b", "c" } }, new[] { "a", "b", "c" }).SetName("ManyInManyOut");
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public async Task TestInputOutput(string[][] inputs, string[] outputs)
        {
            var cts = new CancellationTokenSource(TestTimeout);
            foreach (var batch in inputs)
            {
                await _shard.PublishAsync(batch, cts.Token);
                await Task.Delay(100);
            }

            await ProcessShardHelper.AssertErrorsAndOutputs(
                _shard,
                outputs,
                Array.Empty<string>(),
                cts.Token,
                TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task TestErrorRemoveOldest()
        {
            var toPublish = Enumerable
                .Range(0, ProcessShardOptions.DefaultErrorOptions.Capacity * 2)
                .Select(x => "error " + x)
                .ToList();
            var toAssert = toPublish.Skip(ProcessShardOptions.DefaultErrorOptions.Capacity).ToArray();
            var cts = new CancellationTokenSource(TestTimeout);

            await _shard.PublishAsync(toPublish, cts.Token);
            await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
            await ProcessShardHelper.AssertErrorsAndOutputs(_shard, new string[0], toAssert, cts.Token,
                TimeSpan.FromSeconds(1));
        }

        [OneTimeSetUp]
        public void Setup()
        {

            _cts = new CancellationTokenSource();
            _shard = CreateTestShard("stream");
            _task = _shard.RunAsync(_cts.Token);
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            _cts.Cancel();
            await _task.ConfigureAwait(false);
        }

        private static IShard<string, string, string> CreateTestShard(string mode)
        {
            var logger = new TestLogger();
            return new RestartInfinitelyShard<string, string, string>(
                new ProcessShard<string, string, string>(
                    ProcessShardHelper.CreateTestAppShardOptions(mode),
                    new NewLineDeserializer(),
                    new NewLineDeserializer(),
                    new NewLineSerializer(),
                    logger),
                logger,
                TimeSpan.Zero,
                TimeSpan.Zero);
        }

        private CancellationTokenSource _cts;
        private IShard<string, string, string> _shard;
        private Task _task;
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    }
}