using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.ProxyHost
{
    public class TcpProxy : IProxy
    {
        public async Task StartAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public async Task StopAsync(CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}