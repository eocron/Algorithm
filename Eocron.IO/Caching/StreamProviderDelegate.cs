using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.IO.Caching
{
    public delegate Task<Stream> StreamProviderDelegate(string key, CancellationToken ct = default);
}