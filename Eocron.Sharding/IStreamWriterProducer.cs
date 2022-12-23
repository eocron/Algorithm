using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding;

public interface IStreamWriterSerializer<in T>
{
    Task SerializeTo(StreamWriter writer, T item, CancellationToken ct);
}