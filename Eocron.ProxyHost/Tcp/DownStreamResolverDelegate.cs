using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.ProxyHost.Tcp
{
    public delegate Task<IPEndPoint> DownStreamResolverDelegate(string downStreamHost, int downStreamPort,
        CancellationToken ct);
}