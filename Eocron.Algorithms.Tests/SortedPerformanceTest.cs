using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Eocron.Algorithms.Sorted;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class SortedPerformanceTest
    {
        [Test]
        [Explicit]
        public void Run()
        {
            var config = new ManualConfig()
                .WithOptions(ConfigOptions.DisableOptimizationsValidator)
                .AddValidator(JitOptimizationsValidator.DontFailOnError)
                .AddLogger(ConsoleLogger.Default)
                .AddColumnProvider(DefaultColumnProviders.Instance);
            BenchmarkRunner.Run<BenchmarkSuit>(config);
        }

        [Orderer(SummaryOrderPolicy.SlowestToFastest, MethodOrderPolicy.Alphabetical)]
        //[HardwareCounters(
        //    HardwareCounter.BranchMispredictions,
        //    HardwareCounter.BranchInstructions)]
        [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
        [CategoriesColumn]
        public class BenchmarkSuit
        {
            [Benchmark]
            public int Search()
            {
                return _array.BinarySearchIndexOf(_array[TestDataId]);
            }

            [Benchmark]
            public int SearchLower()
            {
                return _array.LowerBoundIndexOf(_array[TestDataId]);
            }

            [Benchmark]
            public int SearchUpper()
            {
                return _array.UpperBoundIndexOf(_array[TestDataId]);
            }


            #region Setup

            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random();
                _array = Enumerable.Range(0, Size).Select(_ => rnd.Next()).OrderBy(x => x).ToArray();
            }

            [Params(0, Size - 1, Size / 2, Size / 4, Size - Size / 4)]
            public int TestDataId;

            private int[] _array;

            private const int Size = 1 << 20; //20 hops

            #endregion
        }
    }
}