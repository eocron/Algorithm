using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NTests.Core
{
    public static class Benchmark
    {
        /// <summary>
        /// Performs infinite measure of method execution with warmup, until cancellation is requested.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="token">Cancellation token to cancel infinite measure</param>
        /// <param name="warmup"></param>
        public static void InfiniteMeasure(Action<BenchmarkContext> subject, CancellationToken token, bool warmup = true)
        {
            if (token == CancellationToken.None)
                throw new ArgumentOutOfRangeException(nameof(token));


            if (warmup)
            {
                for (int i = 0; i < 3; i++)
                {
                    subject(new BenchmarkContext());     // warm up
                }
            }
            var results = new List<double>(10000);
            var watch = new BenchmarkContext();

            GC.Collect();  // compact Heap
            GC.WaitForPendingFinalizers(); // and wait for the finalizer queue to empty
            GC.Collect();
            while(!token.IsCancellationRequested)
            {
                watch.Start();
                try
                {
                    subject(watch);
                }
                catch (OperationCanceledException)
                {
                    break;//ignoring measurement
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
            Console.WriteLine("Med:\t{0:F0} op/sec", results[results.Count/2]);
        }

        /// <summary>
        /// Performs infinite measure of method execution with warmup, until cancellation is requested.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="token">Cancellation token to cancel infinite measure</param>
        /// <param name="warmup"></param>
        public static async Task InfiniteMeasureAsync(Func<BenchmarkContext, Task> subject, CancellationToken token, bool warmup = true)
        {
            if (token == CancellationToken.None)
                throw new ArgumentOutOfRangeException(nameof(token));


            if (warmup)
            {
                for (int i = 0; i < 3; i++)
                {
                    await subject(new BenchmarkContext()).ConfigureAwait(false);     // warm up
                }
            }
            var results = new List<double>(10000);
            var watch = new BenchmarkContext();

            GC.Collect();  // compact Heap
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
                    break;//ignoring measurement
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
