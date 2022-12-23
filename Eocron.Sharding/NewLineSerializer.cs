using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public sealed class NewLineSerializer : IStreamWriterSerializer<string>
    {
        public async Task SerializeTo(StreamWriter writer, IEnumerable<string> items, CancellationToken ct)
        {
            foreach (var item in items)
            {
                await writer.WriteLineAsync(item).ConfigureAwait(false);
            }
        }
    }
}