using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.HashCode
{

    public static class FileHashCodeExtensions
    {
        /// <summary>
        /// Retrieves efficient file hash code.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<long> GetHashCodeAsync(this FileInfo fileInfo, CancellationToken cancellationToken = default)
        {
            await using var fs = fileInfo.OpenRead();
            return await fs.GetHashCodeAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Retrieves efficient file hash code.
        /// </summary>
        /// <param name="fileInfo"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static long GetHashCode(this FileInfo fileInfo, CancellationToken cancellationToken = default)
        {
            using var fs = fileInfo.OpenRead();
            return fs.GetHashCode(cancellationToken);
        }
    }
}
