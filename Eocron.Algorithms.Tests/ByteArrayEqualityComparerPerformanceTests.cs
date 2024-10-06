using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;
using Eocron.Algorithms.EqualityComparers;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    [Category("Performance")]
    [Explicit]
    public class ByteArrayEqualityComparerPerformanceTests
    {
        [Test]
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
            #region fast

            [BenchmarkCategory("GetHashCode")]
            [Benchmark(Baseline = false)]
            public int GetHashCode_fast()
            {
                return _fastComparer.GetHashCode(_sets[TestDataId].Data);
            }

            [BenchmarkCategory("Equals")]
            [Benchmark(Baseline = false)]
            public bool Equals_fast()
            {
                return _fastComparer.Equals(_sets[TestDataId].Data, _sets[TestDataId].DataEqual);
            }

            [BenchmarkCategory("NotEquals")]
            [Benchmark(Baseline = false)]
            public bool NotEquals_fast()
            {
                return _fastComparer.Equals(_sets[TestDataId].Data, _sets[TestDataId].DataNotEqual);
            }

            #endregion

            #region Base64Equivalent

            [BenchmarkCategory("GetHashCode")]
            [Benchmark(Baseline = true)]
            public int GetHashCode_Base64String()
            {
                return _sets[TestDataId].DataAsString.GetHashCode();
            }

            [BenchmarkCategory("Equals")]
            [Benchmark(Baseline = true)]
            public bool Equals_Base64String()
            {
                return _sets[TestDataId].DataAsString.Equals(_sets[TestDataId].DataEqualAsString);
            }

            [BenchmarkCategory("NotEquals")]
            [Benchmark(Baseline = true)]
            public bool NotEquals_Base64String()
            {
                return _sets[TestDataId].DataAsString.Equals(_sets[TestDataId].DataNotEqualAsString);
            }

            #endregion

            /*#region SequenceEquals

            [BenchmarkCategory("Equals"), Benchmark(Baseline = false)]
            public bool Equals_SequenceEquals()
            {
                return _sets[TestDataId].Data.SequenceEqual(_sets[TestDataId].DataEqual);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = false)]
            public bool NotEquals_SequenceEquals()
            {
                return _sets[TestDataId].Data.SequenceEqual(_sets[TestDataId].DataNotEqual);
            }

            #endregion*/

            #region Setup

            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random();
                _sets = new[]
                {
                    new BenchmarkTestData(15, rnd),
                    new BenchmarkTestData(16 * 1024, rnd)
                };
                _fastComparer = new ByteArrayEqualityComparer();
            }

            [Params(0, 1)] public int TestDataId;

            public class BenchmarkTestData
            {
                public BenchmarkTestData(int size, Random rnd)
                {
                    Data = new byte[size];
                    DataNotEqual = new byte[size];
                    DataEqual = new byte[size];
                    rnd.NextBytes(DataNotEqual);
                    rnd.NextBytes(Data);
                    Array.Copy(Data, DataEqual, size);
                    DataAsString = Convert.ToBase64String(Data);
                    DataEqualAsString = Convert.ToBase64String(DataEqual);
                    DataNotEqualAsString = Convert.ToBase64String(DataNotEqual);
                }

                public readonly byte[] Data;
                public readonly byte[] DataEqual;
                public readonly byte[] DataNotEqual;
                public readonly string DataAsString;
                public readonly string DataEqualAsString;
                public readonly string DataNotEqualAsString;
            }

            private IEqualityComparer<byte[]> _fastComparer;
            private BenchmarkTestData[] _sets;

            #endregion
        }
    }
}