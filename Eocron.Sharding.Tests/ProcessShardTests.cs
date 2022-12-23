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
            using var shard = CreateTestShard("stream");
            var task = shard.RunAsync(cts.Token);
            await shard.PublishAsync(new[] { "hang" }, cts.Token);
            await shard.PublishAsync(new[] { "test" }, cts.Token);
            cts.Cancel();

            await task;

            await ProcessShardHelper.AssertErrorsAndOutputs(
                shard, 
                Array.Empty<string>(), 
                Array.Empty<string>(), 
                CancellationToken.None,
                TimeSpan.FromSeconds(1));
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
        
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);
    }
}
