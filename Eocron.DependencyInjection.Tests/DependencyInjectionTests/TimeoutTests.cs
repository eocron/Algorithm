using System;
using System.Threading;
using System.Threading.Tasks;
using Eocron.DependencyInjection.Interceptors;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests.DependencyInjectionTests
{
    public class TimeoutTests : BaseDependencyInjectionTests
    {
        [Test]
        public async Task Pessimistic()
        {
            long counter = 0;
            Instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<int, CancellationToken>(async (_,ct) =>
                {
                    while (true)
                    {
                        Interlocked.Increment(ref counter);
                        await Task.Delay(10);
                    }

                    return 111;
                });
            
            var proxy = CreateTestObject(x=> x.AddTimeout(Timeout));
            var func = async () => await proxy.WorkWithResultAsync(11, Ct);
            await func.Should().ThrowAsync<TimeoutException>();
            var p1 = counter;
            await Task.Delay(200);
            var p2 = counter;

            p2.Should().BeGreaterThan(p1);
        }
        
        [Test]
        public async Task Optimistic()
        {
            long counter = 0;
            Instance.Setup(x => x.WorkWithResultAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<int, CancellationToken>(async (_,ct) =>
                {
                    while (true)
                    {
                        Interlocked.Increment(ref counter);
                        await Task.Delay(10, ct);
                    }

                    return 111;
                });
            
            var proxy = CreateTestObject(x=> x.AddTimeout(Timeout));
            var func = async () => await proxy.WorkWithResultAsync(11, Ct);
            await func.Should().ThrowAsync<TimeoutException>();
            var p1 = counter;
            await Task.Delay(200);
            var p2 = counter;
            await Task.Delay(200);
            var p3 = counter;
            new []{p1,p2,p3}.Should().Equal(new []{p1,p1,p1});
        }

        public TimeSpan Timeout = TimeSpan.FromSeconds(1);
    }
}