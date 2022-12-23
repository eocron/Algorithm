using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Eocron.Sharding;

public interface IStreamReaderDeserializer<out T>
{
    IAsyncEnumerable<T> GetDeserializedEnumerableAsync(StreamReader reader, CancellationToken ct);
}