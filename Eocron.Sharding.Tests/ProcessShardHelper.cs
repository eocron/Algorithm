using NUnit.Framework;
using System.Diagnostics;

namespace Eocron.Sharding.Tests
{
    public static class ProcessShardHelper
    {
        public static Task AssertErrorsAndOutputs<TInput, TOutput, TError>(
            IShard<TInput, TOutput, TError> shard,
            TOutput[] outputs, TError[] errors,
            CancellationToken ct,
            TimeSpan forTime)
        {
            return Task.WhenAll(
                AssertIsEqual(shard.Outputs.AsAsyncEnumerable(ct), forTime, outputs),
                AssertIsEqual(shard.Errors.AsAsyncEnumerable(ct), forTime, errors));
        }
        public static Task AssertIsEmpty<T>(IAsyncEnumerable<T> enumerable, TimeSpan forTime)
        {
            return AssertIsEqual(enumerable, forTime);
        }
        public static async Task AssertIsEqual<T>(IAsyncEnumerable<T> enumerable, TimeSpan forTime, params T[] expected)
        {
            expected = expected ?? Array.Empty<T>();
            var result = await ConsumeFor(enumerable, forTime).ConfigureAwait(false);
            CollectionAssert.AreEqual(expected, result);
        }
        private static async Task<List<T>> ConsumeFor<T>(IAsyncEnumerable<T> enumerable, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            var result = new List<T>();
            try
            {
                await foreach (var i in enumerable.WithCancellation(cts.Token))
                {
                    result.Add(i);
                }
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
            }

            return result;
        }
        public static ProcessStartInfo CreateTestAppInfo(string mode)
        {
            return new ProcessStartInfo("Tools/Eocron.Sharding.TestApp.exe") { ArgumentList = { mode } }.ConfigureAsService();
        }
    }
}
