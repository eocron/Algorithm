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
    [Ignore("Not yet tested")]
    public class RetryUntilConditionInterceptorTests
    {
        private IAsyncInterceptor _interceptor;
        private IAsyncInterceptor _interceptorWithDelay;

        [SetUp]
        public void Setup()
        {
            _interceptor = new RetryUntilConditionAsyncInterceptor((_, _) => true, (_, _) => TimeSpan.Zero, TestConsoleLogger.Instance);
            _interceptorWithDelay = new RetryUntilConditionAsyncInterceptor((_, _) => true, (_, _) => TimeSpan.FromSeconds(10), TestConsoleLogger.Instance);
        }
        [Test]
        public async Task WorkAsync()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            var token = cts.Token;
            instance.SetupSequence(x => x.WorkAsync(It.IsAny<int>(), It.Is<CancellationToken>(y=> y == token)))
                .ThrowsAsync(new Exception())
                .Returns(Task.CompletedTask);
            instance.SetupSequence(x => x.WorkAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception())
                .Returns(Task.CompletedTask);
            instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>(), It.Is<CancellationToken>(y=> y == token)))
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            instance.SetupSequence(x => x.WorkWithResultAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception())
                .ReturnsAsync(2);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            await proxy.WorkAsync(1, token);
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            await proxy.WorkAsync(1);
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>()), Times.Exactly(2));
            (await proxy.WorkWithResultAsync(1, token)).Should().Be(2);
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            (await proxy.WorkWithResultAsync(1)).Should().Be(2);
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>()), Times.Exactly(2));
        }
    
        [Test]
        public void Work()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            instance.SetupSequence(x => x.Work(It.IsAny<int>()))
                .Throws(new Exception())
                .Pass();
            instance.SetupSequence(x => x.WorkWithResult(It.IsAny<int>()))
                .Throws(new Exception())
                .Returns(2);
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            proxy.Work(1);
            instance.Verify(x=> x.Work(It.IsAny<int>()), Times.Exactly(2));
            proxy.WorkWithResult(1).Should().Be(2);
            instance.Verify(x=> x.WorkWithResult(It.IsAny<int>()), Times.Exactly(2));
        }

        [Test]
        public async Task WorkAsyncWithCancellation()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(1000);
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.Is<CancellationToken>(y => y == token)))
                .ThrowsAsync(new Exception());
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.Is<CancellationToken>(y=> y == token)))
                .ThrowsAsync(new Exception());
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptor);
            var action = async ()=> await Task.WhenAll(
                proxy.WorkAsync(1, token),
                proxy.WorkWithResultAsync(1, token));

            await action.Should().ThrowAsync<Exception>();
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
        }
    
        [Test]
        public async Task WorkAsyncWithLengthyCancellation()
        {
            var instance = new Mock<ITest>(MockBehavior.Strict);
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(1000);
            var token = cts.Token;
            instance.Setup(x => x.WorkAsync(It.IsAny<int>(), It.Is<CancellationToken>(y => y == token)))
                .ThrowsAsync(new Exception());
            instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.Is<CancellationToken>(y=> y == token)))
                .ThrowsAsync(new Exception());
            var proxy = InterceptionHelper.CreateProxy(instance.Object, _interceptorWithDelay);
            var action = async ()=> await Task.WhenAll(
                proxy.WorkAsync(1, token),
                proxy.WorkWithResultAsync(1, token));
        
            await action.Should().ThrowAsync<Exception>();
        
            instance.Verify(x=> x.WorkAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
            instance.Verify(x=> x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(1));
        }
    }
}