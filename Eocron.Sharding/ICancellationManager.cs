using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Sharding
{
    public interface ICancellationManager
    {
        /// <summary>
        ///     Cancel or wait until it started and cancel
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task CancelAsync(CancellationToken ct);

        bool TryCancel();
    }
}