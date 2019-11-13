using System.IO;
using System.Threading;

namespace Algorithm.FileCache
{
    public interface IFileSystem
    {
        bool FileExist(string path, CancellationToken token);
        bool DirectoryExist(string path, CancellationToken token);
        /// <summary>
        /// Moves file/directory from target to source
        /// </summary>
        /// <param name="src"></param>
        /// <param name="tgt"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void Move(string src, string tgt, CancellationToken token);

        void CopyFile(string src, string tgt, CancellationToken token);

        void CreateHardLink(string src, string tgt, CancellationToken token);

        Stream OpenRead(string path, CancellationToken token);

        Stream OpenCreate(string path, CancellationToken token);

        Stream OpenWrite(string path, CancellationToken token);

        bool Equals(string firstPath, string secondPath, CancellationToken token);

        /// <summary>
        /// Deletes file
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void DeleteFile(string path, CancellationToken token);

        /// <summary>
        /// Deletes directory. Non recursive. If file or dir inside - it will throw exception.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        void DeleteDirectoryNonRecursive(string path, CancellationToken token);

        string[] GetFiles(string path, string pattern, SearchOption option, CancellationToken token);
        string[] GetDirectories(string path, string pattern, SearchOption option, CancellationToken token);

        void CreateDirectory(string path, CancellationToken token);
        void SetAttributes(string filePath, FileAttributes normal, CancellationToken t);
    }
}
