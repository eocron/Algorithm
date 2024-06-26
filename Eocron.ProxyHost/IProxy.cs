using System.Net;
using Microsoft.Extensions.Hosting;

namespace Eocron.ProxyHost;

public interface IProxy : IHostedService
{
    EndPoint UpStreamEndpoint { get; }
}