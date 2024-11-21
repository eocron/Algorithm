using System;
using System.IO;

namespace Eocron.Algorithms.Caching
{
    public interface IFileCacheLink : IAsyncDisposable
    {
        Stream OpenRead();

        /// <summary>
        ///     This is only for unmanaged access to file on file system. ANY change to this path can corrupt cache state.
        /// </summary>
        string UnsafeFilePath { get; }
    }
}