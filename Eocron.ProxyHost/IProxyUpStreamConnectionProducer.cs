using System.Collections.Generic;
using System.Net;
using System.Threading;

namespace Eocron.ProxyHost;

public interface IProxyUpStreamConnectionProducer 
{
    EndPoint UpStreamEndpoint { get; }
    IAsyncEnumerable<IProxyConnection> GetPendingConnections(CancellationToken ct);
}