using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public interface IStreamWriterSerializer<in T>
    {
        Task SerializeTo(StreamWriter writer, IEnumerable<T> item, CancellationToken ct);
    }
}