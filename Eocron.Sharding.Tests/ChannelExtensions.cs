using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Eocron.Sharding.Tests
{
    public static class ChannelExtensions
    {
        public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this ChannelReader<T> output,
            [EnumeratorCancellation] CancellationToken ct)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            while (await output.WaitToReadAsync(ct).ConfigureAwait(false))
            {
                yield return await output.ReadAsync(ct).ConfigureAwait(false);
            }
        }
    }
}