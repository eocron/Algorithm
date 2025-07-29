using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests.DependencyInjectionTests
{
    public abstract class BaseDependencyInjectionTests
    {
        protected Mock<ITest> Instance;
        protected CancellationToken Ct => CancellationToken.None;

        [SetUp]
        public void Setup()
        {
            Instance = new Mock<ITest>();
        }

        protected ITest CreateTestObject(Action<DecoratorChain> chainBuilder)
        {
            var sc = new ServiceCollection();
            sc.AddSingleton<ILoggerFactory>(new TestConsoleLoggerFactory());
            sc.AddSingleton<ITest>(Instance.Object, chainBuilder);
            var sp = sc.BuildServiceProvider();
            return sp.GetRequiredService<ITest>();
        }
    }
}