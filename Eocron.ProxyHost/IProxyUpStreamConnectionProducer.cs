using System.Collections.Generic;
using System.Threading;

namespace Eocron.ProxyHost;

public interface IProxyUpStreamConnectionProducer 
{
    IAsyncEnumerable<IProxyConnection> GetPendingConnections(CancellationToken ct);
}