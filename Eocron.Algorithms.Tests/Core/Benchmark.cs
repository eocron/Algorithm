using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Tests.Core
{
    public static class Benchmark
    {
        /// <summary>
        ///     Performs infinite measure of method execution with warmup, until cancellation is requested.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="token">Cancellation token to cancel infinite measure</param>
        /// <param name="warmup"></param>
        public static void InfiniteMeasure(Action<BenchmarkContext> subject, CancellationToken token,
            bool warmup = true)
        {
            if (token == CancellationToken.None)
                throw new ArgumentOutOfRangeException(nameof(token));


            if (warmup)
                for (var i = 0; i < 3; i++)
                    subject(new BenchmarkContext()); // warm up
            var results = new List<double>(10000);
            var memoryResults = new List<long>(10000);
            var watch = new BenchmarkContext();

            GC.Collect(); // compact Heap
            GC.WaitForPendingFinalizers(); // and wait for the finalizer queue to empty
            GC.Collect();
            while (!token.IsCancellationRequested)
            {
                var prev = GC.GetTotalMemory(false);
                watch.Start();
                try
                {
                    subject(watch);
                }
                catch (OperationCanceledException)
                {
                    break; //ignoring measurement
                }
                finally
                {
                    watch.Stop();
                }

                var next = GC.GetTotalMemory(false);
                results.Add(watch.TotalCount / watch.Stopwatch.Elapsed.TotalSeconds);
                memoryResults.Add(prev - next);
            }

            results.Sort();
            memoryResults.Sort();

            Console.WriteLine("Rps:");
            Console.WriteLine("Max:\t{0:F0} rps", results.Last());
            Console.WriteLine("Min:\t{0:F0} rps", results.First());
            Console.WriteLine("Avg:\t{0:F0} rps", results.Sum() / results.Count);
            Console.WriteLine("Med:\t{0:F0} rps", results[results.Count / 2]);

            Console.WriteLine("Memory:");
            Console.WriteLine("Max:\t{0:F0} bps", memoryResults.Last());
            Console.WriteLine("Min:\t{0:F0} bps", memoryResults.First());
            Console.WriteLine("Avg:\t{0:F0} bps", memoryResults.Sum() / memoryResults.Count);
            Console.WriteLine("Med:\t{0:F0} bps", memoryResults[memoryResults.Count / 2]);
        }


        /// <summary>
        ///     Performs infinite measure of method execution with warmup, until cancellation is requested.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="token">Cancellation token to cancel infinite measure</param>
        /// <param name="warmup"></param>
        public static async Task InfiniteMeasureAsync(Func<BenchmarkContext, Task> subject, CancellationToken token,
            bool warmup = true)
        {
            if (token == CancellationToken.None)
                throw new ArgumentOutOfRangeException(nameof(token));


            if (warmup)
                for (var i = 0; i < 3; i++)
                    await subject(new BenchmarkContext()).ConfigureAwait(false); // warm up
            var results = new List<double>(10000);
            var watch = new BenchmarkContext();

            GC.Collect(); // compact Heap
            GC.WaitForPendingFinalizers(); // and wait for the finalizer queue to empty
            GC.Collect();
            while (!token.IsCancellationRequested)
            {
                watch.Start();
                try
                {
                    await subject(watch).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break; //ignoring measurement
                }
                finally
                {
                    watch.Stop();
                }

                results.Add(watch.TotalCount / watch.Stopwatch.Elapsed.TotalSeconds);
            }

            results.Sort();
            Console.WriteLine("Max:\t{0:F0} op/sec", results.Last());
            Console.WriteLine("Min:\t{0:F0} op/sec", results.First());
            Console.WriteLine("Avg:\t{0:F0} op/sec", results.Sum() / results.Count);
            Console.WriteLine("Med:\t{0:F0} op/sec", results[results.Count / 2]);
        }
    }
}