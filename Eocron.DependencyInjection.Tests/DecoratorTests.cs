﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace Eocron.DependencyInjection.Tests
{
    [TestFixture]
    public class DecoratorTests
    {
        [Test]
        public void Decoration()
        {
            var mock = new Mock<ITest>();
            var sc = new ServiceCollection();
            sc.AddTransient<ITest>(_=> mock.Object,
                c => c
                    .Add((sp,_, o, _) => o)
                    .Add((sp,_, o, _) => o));
            
            sc.Select(x => x.ServiceKey).Should().Equal(["000001_decorator", "000002_decorator", null]);
            sc.Should().ContainSingle(x=> x.ServiceType == typeof(ITest) && x.Lifetime == ServiceLifetime.Transient && !x.IsKeyedService);
        }
        
        [Test]
        public void NoDecoration()
        {
            var mock = new Mock<ITest>();
            var sc = new ServiceCollection();
            sc.AddTransient<ITest>(_=> mock.Object,
                c => {});
            
            sc.Should().ContainSingle(x=> x.ServiceType == typeof(ITest) && x.Lifetime == ServiceLifetime.Transient && !x.IsKeyedService);
            sc.Select(x => x.ServiceKey).Should().Equal([null]);
        }
        
        [Test]
        public void DecorationExecutionOrder()
        {
            var mock = new Mock<ITest>();
            var sb = new StringBuilder();
            mock.Setup(x => x.Work(It.IsAny<int>())).Callback<int>((x) =>
            {
                sb.Append("implementation_call_"+x);
            });
            var sc = new ServiceCollection();
            sc.AddTransient<ITest>(_=> mock.Object,
                c => c
                    .Add((sp,_, o, _) =>
                    {
                        sb.Append("second_call ");
                        return o;
                    })
                    .Add((sp,_, o, _) =>                     
                    {
                        sb.Append("first_call ");
                        return o;
                    }));

            var sp = sc.BuildServiceProvider();
            var res = sp.GetService<ITest>();
            res.Work(111);
            sb.ToString().Should().Be("first_call second_call implementation_call_111");
        }
    }
}