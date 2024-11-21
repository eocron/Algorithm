using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.Caching
{
    public delegate Task<string> FilePathProviderDelegate(string key, CancellationToken ct = default);
}