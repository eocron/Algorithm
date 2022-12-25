using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding.Jobs
{
    public interface IJob : IDisposable
    {
        Task RunAsync(CancellationToken ct);
    }
}