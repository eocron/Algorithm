using Eocron.Sharding.Jobs;
using Moq;
using NUnit.Framework;

namespace Eocron.Sharding.Tests
{
    public class ShardLifetimeJobTests
    {
        private CancellationTokenSource _cts;
        private Mock<IJob> _jobMock;
        private ShardLifetimeJob _job;
        private Task _task;

        [SetUp]
        public async Task Setup()
        {
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            _jobMock = new Mock<IJob>();
            _jobMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>((ct)=>Task.Delay(Timeout.Infinite, ct));
            var logger = new TestLogger();
            _job = new ShardLifetimeJob(_jobMock.Object, logger, true);
            _task = _job.RunAsync(_cts.Token);
            await Task.Delay(1);
        }

        [TearDown]
        public async Task TearDown()
        {
            _cts.Cancel();
            try
            {
                await _task;
            }
            catch (OperationCanceledException) { }
        }

        [Test]
        public async Task RunErrorRun()
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var jobMock = new Mock<IJob>();
            jobMock.Setup(x => x.RunAsync(It.IsAny<CancellationToken>())).Throws(new Exception("TEST"));
            var logger = new TestLogger();
            var job = new ShardLifetimeJob(jobMock.Object, logger, true);
            Assert.ThrowsAsync<Exception>(() => job.RunAsync(cts.Token));
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));//check if it is not stopped manually
            Assert.ThrowsAsync<Exception>(() => job.RunAsync(cts.Token));
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
        }

        [Test]
        public async Task StartStopStart()
        {
            VerifyRunCount(1);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
            await _job.StopAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(1);
            Assert.IsTrue(await _job.IsStoppedAsync(_cts.Token));
            await _job.StartAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(2);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
        }

        [Test]
        public async Task StartRestartRestart()
        {
            VerifyRunCount(1);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
            await _job.RestartAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(2);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
            await _job.RestartAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(3);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
        }

        [Test]
        public async Task StartStart()
        {
            VerifyRunCount(1);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
            await _job.StartAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(1);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
        }

        [Test]
        public async Task StartStopStop()
        {
            VerifyRunCount(1);
            Assert.IsFalse(await _job.IsStoppedAsync(_cts.Token));
            await _job.StopAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(1);
            Assert.IsTrue(await _job.IsStoppedAsync(_cts.Token));
            await _job.StopAsync(_cts.Token);
            await Task.Delay(1);
            VerifyRunCount(1);
            Assert.IsTrue(await _job.IsStoppedAsync(_cts.Token));
        }

        private void VerifyRunCount(int count)
        {
            _jobMock.Verify(x => x.RunAsync(It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}
