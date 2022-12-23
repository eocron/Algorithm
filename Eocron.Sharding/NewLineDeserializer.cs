using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Eocron.Sharding
{
    public sealed class NewLineDeserializer : IStreamReaderDeserializer<string>
    {
        public async IAsyncEnumerable<string> GetDeserializedEnumerableAsync(StreamReader reader, [EnumeratorCancellation] CancellationToken ct)
        {
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();
                yield return await reader.ReadLineAsync().ConfigureAwait(false);
            }
        }
    }
}
