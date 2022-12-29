using Moq;
using NUnit.Framework;

namespace Eocron.Sharding.Tests
{
    [TestFixture]
    public class ProcessShardTests
    {
        [Test]
        public async Task CantStart()
        {
            var cts = new CancellationTokenSource(TestTimeout);
            var handle = new Mock<ProcessShardHelper.ITestProcessJobHandle>();
            using var shard = ProcessShardHelper.CreateTestShard("ErrorImmediately", handle.Object);
            var task = shard.RunAsync(cts.Token);
            await shard.PublishAsync(new[] { "a", "b", "c" }, cts.Token);
            await Task.Delay(1);
            cts.Cancel();
            await task;
            handle.Verify(x=> x.OnStopped(), Times.Exactly(1));
            handle.Verify(x=> x.OnStarting(), Times.AtLeast(2));
        }

        [Test]
        public async Task Hang()
        {
            var cts = new CancellationTokenSource(TestTimeout);
            using var shard = ProcessShardHelper.CreateTestShard("stream");
            var task = shard.RunAsync(cts.Token);
            await shard.PublishAsync(new[] { "hang" }, cts.Token);
            await shard.PublishAsync(new[] { "test" }, cts.Token);
            await Task.Delay(1);
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
        public async Task HangRestart()
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
