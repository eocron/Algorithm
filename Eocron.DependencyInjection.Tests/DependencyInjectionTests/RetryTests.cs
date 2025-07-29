using System;
using System.Threading;
using System.Threading.Tasks;
using Eocron.DependencyInjection.Interceptors;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests.DependencyInjectionTests
{
    [TestFixture]
    public class RetryTests : BaseDependencyInjectionTests
    {
        [Test]
        public async Task ConstantRetryFail()
        {
            Instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException())
                .ThrowsAsync(new InvalidOperationException())
                .ThrowsAsync(new InvalidOperationException())
                .ThrowsAsync(new InvalidOperationException())
                .ThrowsAsync(new InvalidOperationException())
                .ReturnsAsync(2);
            
            var proxy = CreateTestObject(x=> x.AddConstantBackoff(MaxAttempts, MinDelay, jittered: false));
            var func = async () => await proxy.WorkWithResultAsync(11, Ct);
            await func.Should().ThrowAsync<InvalidOperationException>();
        }
        
        [Test]
        public async Task ConstantRetry()
        {
            Instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            
            var proxy = CreateTestObject(x=> x.AddConstantBackoff(MaxAttempts, MaxDelay, jittered: false));
            (await proxy.WorkWithResultAsync(11, Ct)).Should().Be(2);
        }
        
        [Test]
        public async Task ConstantRetryJittered()
        {
            Instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            
            var proxy = CreateTestObject(x=> x.AddConstantBackoff(MaxAttempts, MaxDelay, jittered: true));
            (await proxy.WorkWithResultAsync(11, Ct)).Should().Be(2);
        }
        
        [Test]
        public async Task ExponentialRetry()
        {
            Instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            
            var proxy = CreateTestObject(x=> x.AddExponentialBackoff(MaxAttempts, MinDelay, MaxDelay, jittered: false));
            (await proxy.WorkWithResultAsync(11, Ct)).Should().Be(2);
        }
        
        [Test]
        public async Task ExponentialRetryJittered()
        {
            Instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception())
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            
            var proxy = CreateTestObject(x=> x.AddExponentialBackoff(MaxAttempts, MinDelay, MaxDelay, jittered: true));
            (await proxy.WorkWithResultAsync(11, Ct)).Should().Be(2);
        }

        public int MaxAttempts = 3;
        public TimeSpan MaxDelay = TimeSpan.FromSeconds(1);
        public TimeSpan MinDelay = TimeSpan.FromMilliseconds(20);
    }
}