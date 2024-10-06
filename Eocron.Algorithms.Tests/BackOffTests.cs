using System;
using System.Collections.Generic;
using Eocron.Algorithms.Backoff;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class BackOffTests
    {
        private Dictionary<string, IBackOffIntervalProvider> _providers;

        [OneTimeSetUp]
        public void Setup()
        {
            _providers = new Dictionary<string, IBackOffIntervalProvider>(StringComparer.OrdinalIgnoreCase)
            {
                { "exponential", new ExponentialBackOffIntervalProvider(TimeSpan.FromMinutes(1), 2) },
                { "linear", new LinearBackOffIntervalProvider(TimeSpan.FromMinutes(1)) },
                {
                    "exponentialClamped",
                    new BackOffBuilder()
                        .WithExponential(TimeSpan.FromMinutes(1), 2)
                        .WithClamp(TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(10))
                        .Build()
                }
            };
        }
        
        [TestCase("exponential" ,0, "00:00:00")]
        [TestCase("exponential" ,1, "00:01:00")]
        [TestCase("exponential" ,2, "00:02:00")]
        [TestCase("exponential" ,3, "00:04:00")]
        [TestCase("linear" ,0, "00:00:00")]
        [TestCase("linear" ,1, "00:01:00")]
        [TestCase("linear" ,2, "00:02:00")]
        [TestCase("linear" ,3, "00:03:00")]
        [TestCase("exponentialClamped" ,0, "00:02:00")]
        [TestCase("exponentialClamped" ,1, "00:02:00")]
        [TestCase("exponentialClamped" ,2, "00:02:00")]
        [TestCase("exponentialClamped" ,3, "00:04:00")]
        [TestCase("exponentialClamped" ,4, "00:08:00")]
        [TestCase("exponentialClamped" ,5, "00:10:00")]
        [TestCase("exponentialClamped" ,6, "00:10:00")]
        public void Test(string providerStr, int n, string timespanStr)
        {
            var timespan = TimeSpan.Parse(timespanStr);
            var provider = _providers[providerStr];

            provider.GetNext(new BackOffContext() { N = n }).Should().Be(timespan);
        }
    }
}