
using Eocron.Algorithms;
using NTests.Core;
using NUnit.Framework;
using System;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;

namespace NTests
{
    [TestFixture, Category("Performance"), Explicit]
    public class ByteArrayEqualityComparerPerformanceTests
    {
        [Test]
        public void Run()
        {
            BenchmarkRunner.Run<BenchmarkSuit>(new DebugBuildConfig());
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
            private ArrayEqualityComparer<byte> _stdComparer;
            private ByteArrayEqualityComparer _fastComparer;

            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random();
                int size = 10 * 1024 * 1024;
                _data = new byte[size];
                _dataNotEqual = new byte[size];
                _dataEqual = new byte[size];
                rnd.NextBytes(_dataNotEqual);
                rnd.NextBytes(_data);
                Array.Copy(_data, _dataEqual, size);
                _stdComparer = new ArrayEqualityComparer<byte>();
                _fastComparer = new ByteArrayEqualityComparer(false);
            }
            
            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = true)]
            public int GetHashCode_std()
            {
                return _stdComparer.GetHashCode(_data);
            }
            
            [BenchmarkCategory("GetHashCode"), Benchmark(Baseline = false)]
            public int GetHashCode_fast()
            {
                return _fastComparer.GetHashCode(_data);
            }
            
            // [BenchmarkCategory("Equals"), Benchmark(Baseline = true)]
            // public bool Equals_std()
            // {
            //     return _stdComparer.Equals(_data, _dataEqual);
            // }
            //
            // [BenchmarkCategory("Equals"), Benchmark(Baseline = false)]
            // public bool Equals_fast()
            // {
            //     return _fastComparer.Equals(_data, _dataEqual);
            // }
            
            // [BenchmarkCategory("NotEquals"), Benchmark(Baseline = true)]
            // public bool NotEquals_std()
            // {
            //     return _stdComparer.Equals(_data, _dataNotEqual);
            // }
            //
            // [BenchmarkCategory("NotEquals"), Benchmark(Baseline = false)]
            // public bool NotEquals_fast()
            // {
            //     return _fastComparer.Equals(_data, _dataNotEqual);
            // }
        }
    }
}
