using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace Algorithm.FileCache
{
    public interface IFileSystem
    {
        Task<bool> FileExistAsync(string path, CancellationToken token);
        Task<bool> DirectoryExistAsync(string path, CancellationToken token);
        /// <summary>
        /// Moves file/directory from target to source
        /// </summary>
        /// <param name="src"></param>
        /// <param name="tgt"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task MoveAsync(string src, string tgt, CancellationToken token);

        Task CopyFileAsync(string src, string tgt, CancellationToken token);

        Task CreateHardLink(string src, string tgt, CancellationToken token);

        Task<Stream> OpenReadAsync(string path, CancellationToken token);

        Task<Stream> OpenCreateAsync(string path, CancellationToken token);

        Task<Stream> OpenWriteAsync(string path, CancellationToken token);

        Task<bool> EqualsAsync(string firstPath, string secondPath, CancellationToken token);

        /// <summary>
        /// Deletes file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task DeleteFileAsync(string path, CancellationToken token);

        /// <summary>
        /// Deletes directory. Non recursive. If file or dir inside - it will throw exception.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        Task DeleteDirectoryNonRecursiveAsync(string path, CancellationToken token);

        Task<string[]> GetFilesAsync(string path, string pattern, SearchOption option, CancellationToken token);
        Task<string[]> GetDirectoriesAsync(string path, string pattern, SearchOption option, CancellationToken token);

        Task CreateDirectoryAsync(string path, CancellationToken token);
    }
}
