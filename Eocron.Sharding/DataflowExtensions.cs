using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace Eocron.Sharding
{
    public static class DataflowExtensions
    {
        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IReceivableSourceBlock<T> output,
            [EnumeratorCancellation] CancellationToken ct)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            while (await output.OutputAvailableAsync(ct).ConfigureAwait(false))
            {
                yield return await output.ReceiveAsync(ct).ConfigureAwait(false);
            }
        }
    }
}