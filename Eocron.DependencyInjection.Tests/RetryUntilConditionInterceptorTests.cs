using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Eocron.DependencyInjection.Interceptors;
using Eocron.DependencyInjection.Interceptors.Retry;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests
{
    [TestFixture]
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
        public void CorrelatedExponentialBackoff_Check()
        {
            var rnd = new Random(42);
            var expectedMs = new[] {5, 10, 20, 40, 80, 160, 320, 640, 1280, 2560, 5120, 10240, 20480, 40960, 60000, 60000, 60000, 60000, 60000, 60000};
            var actualMs = Enumerable.Range(1, 20).Select(x=> (int)CorrelatedExponentialBackoff.Calculate(rnd, x, TimeSpan.Zero, TimeSpan.FromSeconds(60), false).TotalMilliseconds).ToArray();
            actualMs.Should().Equal(expectedMs);
        }
        
        [Test]
        public void CorrelatedExponentialBackoffJittered_Check()
        {
            var rnd = new Random(42);
            var expectedMs = new[] {3, 1, 2, 20, 13, 42, 231, 328, 222, 1948, 1201, 2634, 10354, 13116, 22858, 15614, 31047, 2119, 48848, 34631};
            var actualMs = Enumerable.Range(1, 20).Select(x=> (int)CorrelatedExponentialBackoff.Calculate(rnd, x, TimeSpan.Zero, TimeSpan.FromSeconds(60), true).TotalMilliseconds).ToArray();
            actualMs.Should().Equal(expectedMs);
        }
        
        [Test]
        public void ConstantBackoff_Check()
        {
            var rnd = new Random(42);
            var expectedMs = new[] {60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000, 60000};
            var actualMs = Enumerable.Range(1, 20).Select(x=> (int)ConstantBackoff.Calculate(rnd, TimeSpan.FromSeconds(60), false).TotalMilliseconds).ToArray();
            actualMs.Should().Equal(expectedMs);
        }
        
        [Test]
        public void ConstantBackoffJittered_Check()
        {
            var rnd = new Random(42);
            var expectedMs = new[] {40086, 8454, 7531, 31365, 10106, 15755, 43464, 30775, 10419, 45675, 14075, 15439, 30336, 19213, 22858, 15614, 31047, 2119, 48848, 34631};
            var actualMs = Enumerable.Range(1, 20).Select(x=> (int)ConstantBackoff.Calculate(rnd, TimeSpan.FromSeconds(60), true).TotalMilliseconds).ToArray();
            actualMs.Should().Equal(expectedMs);
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