using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace Algorithm.FileCache
{
    public class FileSystemAsync : IFileSystemAsync
    {
        private static readonly Lazy<IFileSystemAsync> _intance = new Lazy<IFileSystemAsync>(() => new FileSystemAsync(), true);
        public static IFileSystemAsync Instance => _intance.Value;

        public virtual Task<bool> FileExistAsync(string path, CancellationToken token)
        {
            return Task.FromResult(new FileInfo(path).Exists);
        }
        public virtual Task<bool> DirectoryExistAsync(string path, CancellationToken token)
        {
            return Task.FromResult(new DirectoryInfo(path).Exists);
        }

        public virtual Task MoveAsync(string src, string tgt, CancellationToken token)
        {
            var file = new FileInfo(src);
            var dir = new DirectoryInfo(src);

            if (file.Exists)
            {
                file.MoveTo(tgt);
            }
            else if (dir.Exists)
            {
                dir.MoveTo(tgt);
            }
            return Task.CompletedTask;
        }

        public Task CopyFileAsync(string src, string tgt, CancellationToken token)
        {
            var srcInfo = new FileInfo(src);
            var tgtInfo = new FileInfo(tgt);

            srcInfo.CopyTo(tgtInfo.FullName, false);
            return Task.CompletedTask;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private static void InternalCreateHardLink(string src, string tgt)
        {
            var res = CreateHardLink(tgt, src, IntPtr.Zero);
            if (!res)
            {
                var ex = new Win32Exception(Marshal.GetLastWin32Error());
                throw ex;
            }
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken token)
        {
            return Task.FromResult((Stream)File.OpenRead(path));
        }

        public Task<Stream> OpenCreateAsync(string path, CancellationToken token)
        {
            return Task.FromResult((Stream)new FileInfo(path).OpenWrite());
        }

        public Task<Stream> OpenWriteAsync(string path, CancellationToken token)
        {
            return Task.FromResult((Stream)new FileInfo(path).OpenWrite());
        }

        public virtual async Task DeleteFileAsync(string path, CancellationToken token)
        {
            await DeleteFileAsync(new FileInfo(path), token);
        }
        public virtual async Task DeleteDirectoryNonRecursiveAsync(string path, CancellationToken token)
        {
            await DeleteDirectoryNonRecursiveAsync(new DirectoryInfo(path), token);
        }

        public virtual Task<string[]> GetFilesAsync(string path, string pattern, SearchOption option, CancellationToken token)
        {
            return Task.FromResult(new DirectoryInfo(path).GetFiles(pattern, option).Select(x => x.FullName).ToArray());
        }

        public virtual Task<string[]> GetDirectoriesAsync(string path, string pattern, SearchOption option, CancellationToken token)
        {
            return Task.FromResult(new DirectoryInfo(path).GetDirectories(pattern, option).Select(x => x.FullName).ToArray());
        }

        public Task CreateDirectoryAsync(string path, CancellationToken token)
        {
            new DirectoryInfo(path).Create();
            return Task.CompletedTask;
        }

        private Task DeleteFileAsync(FileInfo file, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!file.Exists)
                return Task.CompletedTask;
            file.Attributes = FileAttributes.Normal;
            file.Delete();
            return Task.CompletedTask;
        }
        private Task DeleteDirectoryNonRecursiveAsync(DirectoryInfo targetDir, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!targetDir.Exists)
                return Task.CompletedTask;
            targetDir.Attributes = FileAttributes.Normal;
            targetDir.Delete(false);
            return Task.CompletedTask;
        }

        public Task<bool> EqualsAsync(string firstPath, string secondPath, CancellationToken token)
        {
            return Task.FromResult(Path.GetFullPath(firstPath).Equals(Path.GetFullPath(secondPath)));
        }

        public async Task CreateHardLinkAsync(string src, string tgt, CancellationToken token)
        {
            //hard links possible only on same drive
            //also hard links is very hardcore because of their problems with Access denied behavior, so it is disabled.
            //var possible = Path.GetPathRoot(src) ==
            //               Path.GetPathRoot(tgt);
            //if (possible)
            //{
            //    InternalCreateHardLink(src, tgt);
            //    return;
            //}
            await CopyFileAsync(src, tgt, token);
        }

        public Task SetAttributesAsync(string filePath, FileAttributes attr, CancellationToken token)
        {
            if (File.Exists(filePath))
            {
                new FileInfo(filePath).Attributes = attr;
            }
            else if (Directory.Exists(filePath))
            {
                new DirectoryInfo(filePath).Attributes = attr;
            }
            return Task.CompletedTask;
        }
    }
}
