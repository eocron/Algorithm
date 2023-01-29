
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
        
        [Orderer(SummaryOrderPolicy.SlowestToFastest, MethodOrderPolicy.Declared)]
        //[HardwareCounters(
        //    HardwareCounter.BranchMispredictions,
        //    HardwareCounter.BranchInstructions)]
        [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
        [CategoriesColumn]
        public class BenchmarkSuit
        {
            private byte[] _data;
            private byte[] _dataNotEqual;
            private byte[] _dataEqual;
            private ByteArrayEqualityComparer _fastComparer;
            private string _dataAsString;
            private string _dataEqualAsString;
            private string _dataNotEqualAsString;

            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random();
                int size = 16*1024*1024;
                _data = new byte[size];
                _dataNotEqual = new byte[size];
                _dataEqual = new byte[size];
                rnd.NextBytes(_dataNotEqual);
                rnd.NextBytes(_data);
                Array.Copy(_data, _dataEqual, size);
                
                _dataAsString = Convert.ToBase64String(_data);
                _dataEqualAsString = Convert.ToBase64String(_dataEqual);
                _dataNotEqualAsString = Convert.ToBase64String(_dataNotEqual);
                _fastComparer = new ByteArrayEqualityComparer(false);
            }
            
            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = true)]
            public int GetHashCode_Base64String()
            {
                return _dataAsString.GetHashCode();
            }
            
            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = false)]
            public int GetHashCode_fast()
            {
                return _fastComparer.GetHashCode(_data);
            }

            [BenchmarkCategory("Equals"), Benchmark(Baseline = true)]
            public bool Equals_Base64String()
            {
                return _dataAsString.Equals(_dataEqualAsString);
            }
            
            [BenchmarkCategory("Equals"), Benchmark(Baseline = false)]
            public bool Equals_fast()
            {
                return _fastComparer.Equals(_data, _dataEqual);
            }
            
            [BenchmarkCategory("Equals"), Benchmark(Baseline = false)]
            public bool Equals_SequenceEquals()
            {
                return _data.SequenceEqual(_dataEqual);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = true)]
            public bool NotEquals_Base64String()
            {
                return _dataAsString.Equals(_dataNotEqualAsString);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = false)]
            public bool NotEquals_fast()
            {
                return _fastComparer.Equals(_data, _dataNotEqual);
            }
            
            [BenchmarkCategory("NotEquals"), Benchmark(Baseline = false)]
            public bool NotEquals_SequenceEquals()
            {
                return _data.SequenceEqual(_dataNotEqual);
            }
        }
    }
}
