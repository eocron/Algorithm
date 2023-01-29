
using Eocron.Algorithms;
using NTests.Core;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace NTests
{
    [TestFixture, Category("Performance"), Explicit]
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
        [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByParams)]
        [CategoriesColumn]
        public class BenchmarkSuit
        {
            #region fast

            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = false)]
            public int GetHashCode_fast()
            {
                return _fastComparer.GetHashCode(_sets[TestDataId].Data);
            }
            
            [BenchmarkCategory("Equals"), Benchmark(Baseline = false)]
            public bool Equals_fast()
            {
                return _fastComparer.Equals(_sets[TestDataId].Data, _sets[TestDataId].DataEqual);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = false)]
            public bool NotEquals_fast()
            {
                return _fastComparer.Equals(_sets[TestDataId].Data, _sets[TestDataId].DataNotEqual);
            }

            #endregion
            
            #region Base64Equivalent

            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = true)]
            public int GetHashCode_Base64String()
            {
                return _sets[TestDataId].DataAsString.GetHashCode();
            }
            
            [BenchmarkCategory("Equals"), Benchmark(Baseline = true)]
            public bool Equals_Base64String()
            {
                return _sets[TestDataId].DataAsString.Equals(_sets[TestDataId].DataEqualAsString);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = true)]
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
                    new BenchmarkTestData(16, rnd),
                    new BenchmarkTestData(16 * 1024, rnd),
                };
                _fastComparer = new ByteArrayEqualityComparer(false);
            }

            [Params(0,1)]
            public int TestDataId;
            public class BenchmarkTestData
            {
                public byte[] Data;
                public byte[] DataNotEqual;
                public byte[] DataEqual;
                public string DataAsString;
                public string DataNotEqualAsString;
                public string DataEqualAsString;

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
            }
            private ByteArrayEqualityComparer _fastComparer;
            private BenchmarkTestData[] _sets;
            
            #endregion
        }
    }
}
