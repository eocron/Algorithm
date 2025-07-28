using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Eocron.DependencyInjection.Interceptors;
using FluentAssertions;
using Moq;
using NUnit.Framework;
// ReSharper disable MethodSupportsCancellation

namespace Eocron.DependencyInjection.Tests
{
    [TestFixture]
    [Ignore("Not yet tested")]
    public class TimeoutInterceptorTests
    {
        private IAsyncInterceptor _interceptor;

        [SetUp]
        public void Setup()
        {
            _interceptor = new TimeoutAsyncInterceptor(TimeSpan.FromSeconds(1));
        }
        [Test]
        public async Task Optimistic()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(OptimisticSleep(token)));
            instance.Setup(x => x.WorkAsync(It.IsAny<int>()))
                .Returns(() => Task.FromResult(OptimisticSleep(token)));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(OptimisticSleep(token)));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .Returns(() => Task.FromResult(OptimisticSleep(token)));
            instance.Setup(x => x.Work(It.IsAny<int>()))
                .Callback(()=> OptimisticSleep(token));
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Returns(()=> OptimisticSleep(token));
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            var a1 = async()=> await proxy.WorkAsync(1, token);
            var a2 = async()=> await proxy.WorkAsync(1);
            var a3 = async()=> await proxy.WorkWithResultAsync(1, token);
            var a4 = async()=> await proxy.WorkWithResultAsync(1);
            var a5 = () => proxy.Work(1);
            var a6 = () => proxy.WorkWithResult(1);

            await Task.WhenAll(a1.Should().ThrowAsync<TimeoutException>(),
                a2.Should().ThrowAsync<TimeoutException>(),
                a3.Should().ThrowAsync<TimeoutException>(),
                a4.Should().ThrowAsync<TimeoutException>());

            a5.Should().Throw<TimeoutException>();
            a6.Should().Throw<TimeoutException>();
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.Work(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResult(It.IsAny<int>()), Times.Exactly(1));
        }
        
        [Test]
        public async Task Success()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            instance.Setup(x => x.WorkAsync(It.IsAny<int>()))
                .Returns(Task.CompletedTask);
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .ReturnsAsync(2);
            instance.Setup(x => x.Work(It.IsAny<int>()));
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Returns(2);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            await proxy.WorkAsync(1, token);
            await proxy.WorkAsync(1);
            (await proxy.WorkWithResultAsync(1, token)).Should().Be(2);
            (await proxy.WorkWithResultAsync(1)).Should().Be(2);
            proxy.Work(1);
            proxy.WorkWithResult(1).Should().Be(2);
            
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.Work(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResult(It.IsAny<int>()), Times.Exactly(1));
        }
        
        [Test]
        public async Task Pessimistic()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(PessimisticSleep()));
            instance.Setup(x => x.WorkAsync(It.IsAny<int>()))
                .Returns(() => Task.FromResult(PessimisticSleep()));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(PessimisticSleep()));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .Returns(() => Task.FromResult(PessimisticSleep()));
            instance.Setup(x => x.Work(It.IsAny<int>()))
                .Callback(()=> PessimisticSleep());
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Returns(PessimisticSleep);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            var a1 = async()=> await proxy.WorkAsync(1, token);
            var a2 = async()=> await proxy.WorkAsync(1);
            var a3 = async()=> await proxy.WorkWithResultAsync(1, token);
            var a4 = async()=> await proxy.WorkWithResultAsync(1);
            var a5 = () => proxy.Work(1);
            var a6 = () => proxy.WorkWithResult(1);
            
            await Task.WhenAll(a1.Should().ThrowAsync<TimeoutException>(),
                a2.Should().ThrowAsync<TimeoutException>(),
                a3.Should().ThrowAsync<TimeoutException>(),
                a4.Should().ThrowAsync<TimeoutException>());
            a5.Should().Throw<TimeoutException>();
            a6.Should().Throw<TimeoutException>();
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.Work(It.IsAny<int>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResult(It.IsAny<int>()), Times.Exactly(1));
        }

        private int OptimisticSleep(CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            while (true)
            {
                cts.Token.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private int PessimisticSleep()
        {
            Thread.Sleep(TimeSpan.FromSeconds(5));
            return 0;
        }
    }
}