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
        public void Measure128MbInBinary()
        {
            var logger = new AccumulationLogger();
            var config = ManualConfig.Create(DefaultConfig.Instance)
                .AddLogger(logger)
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            BenchmarkRunner.Run<BenchmarkSuit128MbInBinary>(config);
            Console.WriteLine(logger.GetLog());
        }
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
            public void MergeSortOnDisk()
            {
                foreach (var _ in _data.MergeOrderBy(x => x, _storage, _cmp, _chunkSize))
                {
                }
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
                _storage = new JsonStreamEnumerableStorage<int>();
                _chunkSize = 100000;
            }

            [GlobalCleanup]
            public void Cleanup()
            {
                _storage.Clear();
            }

            [Benchmark]
            public void MergeSortOnDisk()
            {
                foreach (var _ in _data.MergeOrderBy(x => x, _storage, _cmp, _chunkSize))
                {
                }
            }
        }

        [ThreadingDiagnoser]
        [MemoryDiagnoser(false)]
        public class BenchmarkSuit128MbInBinary
        {
            private Comparer<int> _cmp;
            private IEnumerableStorage<int> _storage;
            private int _chunkSize;
            private long _fileSize;
            private long _sequenceSize;
            private Random _rnd;

            [GlobalSetup]
            public void Setup()
            {
                _rnd = new Random(42);
                _fileSize = 128L * 1024 * 1024;
                _sequenceSize = _fileSize / sizeof(int);
                _cmp = Comparer<int>.Default;
                _storage = new BinaryIntEnumerableStorage();
                _chunkSize = 16*1024*1024;
            }

            [GlobalCleanup]
            public void Cleanup()
            {
                _storage.Clear();
            }

            [Benchmark]
            public void MergeSortOnDisk()
            {
                foreach (var _ in GetSequence().MergeOrderBy(x => x, _storage, _cmp, _chunkSize))
                {
                }
            }

            public IEnumerable<int> GetSequence()
            {
                for (long i = 0; i < _sequenceSize; i++)
                {
                    yield return _rnd.Next();
                }
            }
        }
    }
}