using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Eocron.ProxyHost.Tests
{
    [TestFixture]
    public class ProxyIntegrationTests
    {
        [Test, Category("Integration")]
        public async Task StartStop()
        {
            var proxy = new TcpProxyBuilder()
                .ConfigureLogging(x =>
                {
                    x.ClearProviders();
                    x.AddConsole();
                    x.SetMinimumLevel(LogLevel.Trace);
                })
                .Build();

            for(int i = 0; i < 10; i++)
            {
                proxy.StartAsync(CancellationToken.None).Wait();
                proxy.StopAsync(CancellationToken.None).Wait();
            }
        }
    }
}