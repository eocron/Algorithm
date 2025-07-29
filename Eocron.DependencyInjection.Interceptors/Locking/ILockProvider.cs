using System;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.DependencyInjection.Interceptors.Locking
{
    public interface ILockProvider
    {
        Task<IAsyncDisposable> AcquireAsync(CancellationToken ct);
        
        IDisposable Acquire(CancellationToken ct);
    }
}