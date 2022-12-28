using NUnit.Framework;

namespace Eocron.Sharding.Tests
{
    [TestFixture]
    public class ProcessShardTests
    {
        [Test]
        public async Task HangHandling()
        {
            var cts = new CancellationTokenSource(TestTimeout);
            using var shard = ProcessShardHelper.CreateTestShard("stream");
            var task = shard.RunAsync(cts.Token);
            await shard.PublishAsync(new[] { "hang" }, cts.Token);
            await shard.PublishAsync(new[] { "test" }, cts.Token);
            cts.Cancel();

            await task;

            await ProcessShardHelper.AssertErrorsAndOutputs(
                shard, 
                new string[]{"hang"}, 
                Array.Empty<string>(), 
                CancellationToken.None,
                TimeSpan.FromSeconds(1));
        }

        [Test]
        public async Task HangRestartHandling()
        {
            var cts = new CancellationTokenSource(TestTimeout);
            using var shard = ProcessShardHelper.CreateTestShard("stream");
            var task = shard.RunAsync(cts.Token);
            await shard.PublishAsync(new[] { "hang" }, cts.Token);
            await Task.Delay(100);
            await shard.RestartAsync(cts.Token);
            await shard.PublishAsync(new[] { "test" }, cts.Token);
            await Task.Delay(100);
            cts.Cancel();

            await task;

            await ProcessShardHelper.AssertErrorsAndOutputs(
                shard,
                new string[] { "hang", "test" },
                Array.Empty<string>(),
                CancellationToken.None,
                TimeSpan.FromSeconds(1));
        }

        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    }
}
