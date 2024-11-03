using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.EqualityComparers.xxHash;

namespace Eocron.Algorithms.HashCode;

public static class StreamHashCodeExtensions
{
    /// <summary>
    ///     Retrieves partial hash code from seekable stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="pool"></param>
    /// <returns></returns>
    public static int GetHashCode(this Stream stream, CancellationToken cancellationToken = default,
        ArrayPool<byte> pool = null)
    {
        return XxHash64.ComputeHashAsync(stream, 8192, 0, cancellationToken, pool ?? ArrayPool<byte>.Shared).Result
            .GetHashCode();
    }

    /// <summary>
    ///     Retrieves partial hash code from seekable stream.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="pool"></param>
    /// <returns></returns>
    public static async ValueTask<long> GetHashCodeAsync(this Stream stream,
        CancellationToken cancellationToken = default, ArrayPool<byte> pool = null)
    {
        return (await XxHash64.ComputeHashAsync(stream, 8192, 0, cancellationToken, pool ?? ArrayPool<byte>.Shared))
            .GetHashCode();
    }
}