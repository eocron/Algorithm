using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public sealed class NewLineSerializer : IStreamWriterSerializer<string>
    {
        public async Task SerializeTo(StreamWriter writer, string item, CancellationToken ct)
        {
            await writer.WriteLineAsync(item).ConfigureAwait(false);
        }
    }
}