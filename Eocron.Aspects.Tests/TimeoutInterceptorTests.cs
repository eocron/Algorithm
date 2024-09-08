using System;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Eocron.Aspects.Tests
{
    [TestFixture]
    public class TimeoutInterceptorTests
    {
        private IAsyncInterceptor _interceptorWithDelay;

        [SetUp]
        public void Setup()
        {
            _interceptorWithDelay = new TimeoutAsyncInterceptor(TimeSpan.FromSeconds(1));
        }
        [Test]
        public async Task WorkAsyncOptimistic()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(async () => OptimisticSleep(token));
            instance.Setup(x => x.WorkAsync(It.IsAny<int>()))
                .Callback(async () => OptimisticSleep(token));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(async () => OptimisticSleep(token));
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .Callback(async () => OptimisticSleep(token));
            instance.Setup(x => x.Work(It.IsAny<int>()))
                .Callback(async () => OptimisticSleep(token));
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Callback(async () => OptimisticSleep(token));
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptorWithDelay);
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
        public async Task WorkAsyncPessimistic()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(async () => PessimisticSleep());
            instance.Setup(x => x.WorkAsync(It.IsAny<int>()))
                .Callback(async () => PessimisticSleep());
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback(async () => PessimisticSleep());
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .Callback(async () => PessimisticSleep());
            instance.Setup(x => x.Work(It.IsAny<int>()))
                .Callback(async () => PessimisticSleep());
            instance.Setup(x => x.WorkWithResult(It.IsAny<int>()))
                .Callback(async () => PessimisticSleep());
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptorWithDelay);
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

        private void OptimisticSleep(CancellationToken ct)
        {
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(1);
            }
        }

        private void PessimisticSleep()
        {
            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }
}