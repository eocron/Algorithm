using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using NUnit.Framework;

namespace Eocron.Aspects.Tests
{
    [TestFixture]
    public class MemoryCacheInterceptorTests
    {
        private IAsyncInterceptor _interceptor;

        [SetUp]
        public void Setup()
        {
            _interceptor = new MemoryCacheAsyncInterceptor(new MemoryCache(new MemoryCacheOptions()),
                (_, args) => args[0],
                (_, _, entry) => entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(10)));
        }
        [Test]
        public async Task CachingAndKeySharing()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2)
                .ReturnsAsync(3)
                .ReturnsAsync(4);
            instance.SetupSequence(x => x.WorkWithResult(It.IsAny<int>()))
                .Returns(2)
                .Returns(3)
                .Returns(4);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);

            var r1 = await proxy.WorkWithResultAsync(2, token);
            var r2 = await proxy.WorkWithResultAsync(2, token);

            r1.Should().Be(r2);
            
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));

            r1 =  proxy.WorkWithResult(2);
            r2 =  proxy.WorkWithResult(2);

            r1.Should().Be(r2);
            instance.Verify(x => x.WorkWithResult(It.IsAny<int>()), Times.Exactly(1));
        }
        
        [Test]
        public async Task AtMostOnceAsync()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);

            await Parallel.ForAsync(0, 100, token, async (_, ct) =>
            {
                await proxy.WorkWithResultAsync(2, ct);
            });

            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
        
        [Test]
        public void AtMostOnceSync()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Returns(2);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);

            Parallel.For(0, 100, (_) =>
            {
                 proxy.WorkWithResult(2);
            });

            instance.Verify(x=> x.WorkWithResult(It.IsAny<int>()), Times.Exactly(1));
        }
    }
}