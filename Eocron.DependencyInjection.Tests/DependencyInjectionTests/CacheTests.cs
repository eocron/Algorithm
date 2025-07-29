using System;
using System.Threading;
using System.Threading.Tasks;
using Eocron.DependencyInjection.Interceptors;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests.DependencyInjectionTests
{
    public class CacheTests : BaseDependencyInjectionTests
    {
        [Test]
        public async Task AbsoluteExpirationErrorNotCached()
        {
            Instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            var proxy = CreateTestObject(x => x.AddAbsoluteTimeoutCache(Expiration, (method, args) => args[0]));

            var func = async () => await proxy.WorkWithResultAsync(1, Ct);
            await func.Should().ThrowAsync<InvalidOperationException>();
            await func.Should().ThrowAsync<InvalidOperationException>();
            await func.Should().ThrowAsync<InvalidOperationException>();
            Instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
        
        [Test]
        public async Task SlidingExpirationErrorNotCached()
        {
            Instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException());

            var proxy = CreateTestObject(x => x.AddSlidingTimeoutCache(Expiration, (method, args) => args[0]));

            var func = async () => await proxy.WorkWithResultAsync(1, Ct);
            await func.Should().ThrowAsync<InvalidOperationException>();
            await func.Should().ThrowAsync<InvalidOperationException>();
            await func.Should().ThrowAsync<InvalidOperationException>();
            Instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
        }
        
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