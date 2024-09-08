using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Aspects.Tests;

public interface ITest
{
    Task WorkAsync(int argument, CancellationToken ct);

    Task WorkAsync(int argument);
    
    Task<int> WorkWithResultAsync(int argument, CancellationToken ct);
    
    Task<int> WorkWithResultAsync(int argument);

    void Work(int argument);

    int WorkWithResult(int argument);
}