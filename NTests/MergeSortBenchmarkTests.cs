using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using Eocron.Algorithms.Sorted;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture, Explicit]
    public class MergeSortBenchmarkTests
    {

        [Test]
        public void MeasureInJson()
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddLogger(logger)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<BenchmarkSuitInJson>(config);
            Console.WriteLine(logger.GetLog());
        }

        [Test]
        public void MeasureInMemory()
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddLogger(logger)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<BenchmarkSuitInMemory>(config);
            Console.WriteLine(logger.GetLog());
        }

        [ThreadingDiagnoser]
        [MemoryDiagnoser(false)]
        public class BenchmarkSuitInMemory
        {
            private int[] _data;
            private Comparer<int> _cmp;
            private IEnumerableStorage<int> _storage;
            private int _chunkSize;
            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random(42);
                _data = Enumerable.Range(0, 1000000).Select(x => rnd.Next()).ToArray();
                _cmp = Comparer<int>.Default;
                _storage = new InMemoryEnumerableStorage<int>();
                _chunkSize = 100000;
            }

            [GlobalCleanup]
            public void Cleanup()
            {
                _storage.Clear();
            }

            [Benchmark]
            public List<int> MergeSortOnDisk()
            {
                return _data.MergeOrderBy(x => x, _storage, _cmp, _chunkSize).ToList();
            }
        }

        [ThreadingDiagnoser]
        [MemoryDiagnoser(false)]
        public class BenchmarkSuitInJson
        {
            private int[] _data;
            private Comparer<int> _cmp;
            private IEnumerableStorage<int> _storage;
            private int _chunkSize;
            [GlobalSetup]
            public void Setup()
            {
                var rnd = new Random(42);
                _data = Enumerable.Range(0, 1000000).Select(x => rnd.Next()).ToArray();
                _cmp = Comparer<int>.Default;
                _storage = new JsonEnumerableStorage<int>();
                _chunkSize = 100000;
            }

            [GlobalCleanup]
            public void Cleanup()
            {
                _storage.Clear();
            }

            [Benchmark]
            public List<int> MergeSortOnDisk()
            {
                return _data.MergeOrderBy(x => x, _storage, _cmp, _chunkSize).ToList();
            }
        }
    }
}