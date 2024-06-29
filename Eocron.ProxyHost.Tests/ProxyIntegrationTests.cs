using System;
using System.Threading;
using System.Threading.Tasks;
using Eocron.ProxyHost.Tcp;
using FluentAssertions;
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
                await proxy.StartAsync(CancellationToken.None);
                proxy.UpStreamEndpoint.Should().NotBeNull();
                await proxy.StopAsync(CancellationToken.None);
            }
        }
        
        [Test, Category("Integration")]
        public async Task StartCancellation()
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
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                var action = async ()=> await proxy.StartAsync(cts.Token);
                await action.Should().ThrowAsync<OperationCanceledException>();
            }
        }
    }
}