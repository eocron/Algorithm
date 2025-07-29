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

    public class CacheTests : BaseDependencyInjectionTests
    {
        [Test]
        public async Task AbsoluteExpiration()
        {
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var proxy = CreateTestObject(x => x.AddAbsoluteTimeoutCache(Expiration, (method, args) => args[0]));
            
            //first pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);

            await Task.Delay(Expiration);
            
            //second pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(2));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [Test]
        public async Task SlidingExpiration()
        {
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var proxy = CreateTestObject(x => x.AddSlidingTimeoutCache(Expiration, (method, args) => args[0]));
            
            //first pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);

            await Task.Delay(Expiration);
            
            //second pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(2));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);
        }
        
        [Test]
        public async Task SlidingExpirationOverlap()
        {
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>())).ReturnsAsync(1);
            Instance.Setup(x => x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>())).ReturnsAsync(2);

            var proxy = CreateTestObject(x => x.AddSlidingTimeoutCache(Expiration, (method, args) => args[0]));
            
            //first pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);

            await Task.Delay(Expiration/2);
            
            //second pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);
            
            await Task.Delay(Expiration/2);
            
            //third pass
            await Parallel.ForAsync(0, 100, async (_, _) =>
            {
                (await proxy.WorkWithResultAsync(1, Ct)).Should().Be(1);
            });
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==1), It.IsAny<CancellationToken>()), Times.Exactly(1));
            Instance.Verify(x=> x.WorkWithResultAsync(It.Is<int>(i=> i==2), It.IsAny<CancellationToken>()), Times.Never);
        }
        public TimeSpan Expiration = TimeSpan.FromSeconds(3);
    }
}