using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Eocron.Algorithms.FileCache
{
    public class FileSystem : IFileSystem
    {
        public void CopyFile(string src, string tgt, CancellationToken token)
        {
            var srcInfo = new FileInfo(src);
            var tgtInfo = new FileInfo(tgt);

            srcInfo.CopyTo(tgtInfo.FullName, false);
            SetAttributes(tgtInfo.FullName, FileAttributes.Normal, token);
        }

        public void CreateDirectory(string path, CancellationToken token)
        {
            new DirectoryInfo(path).Create();
        }

        public void CreateHardLink(string src, string tgt, CancellationToken token)
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
            CopyFile(src, tgt, token);
        }

        public virtual void DeleteDirectoryNonRecursive(string path, CancellationToken token)
        {
            DeleteDirectoryNonRecursive(new DirectoryInfo(path), token);
        }

        public virtual void DeleteFile(string path, CancellationToken token)
        {
            DeleteFile(new FileInfo(path), token);
        }

        public virtual bool DirectoryExist(string path, CancellationToken token)
        {
            return new DirectoryInfo(path).Exists;
        }

        public bool Equals(string firstPath, string secondPath, CancellationToken token)
        {
            return Path.GetFullPath(firstPath).Equals(Path.GetFullPath(secondPath));
        }

        public virtual bool FileExist(string path, CancellationToken token)
        {
            return new FileInfo(path).Exists;
        }

        public virtual string[] GetDirectories(string path, string pattern, SearchOption option,
            CancellationToken token)
        {
            return new DirectoryInfo(path).GetDirectories(pattern, option).Select(x => x.FullName).ToArray();
        }

        public virtual string[] GetFiles(string path, string pattern, SearchOption option, CancellationToken token)
        {
            return new DirectoryInfo(path).GetFiles(pattern, option).Select(x => x.FullName).ToArray();
        }

        public virtual void Move(string src, string tgt, CancellationToken token)
        {
            var file = new FileInfo(src);
            var dir = new DirectoryInfo(src);

            if (file.Exists)
                file.MoveTo(tgt);
            else if (dir.Exists) dir.MoveTo(tgt);
        }

        public Stream OpenCreate(string path, CancellationToken token)
        {
            return new FileInfo(path).OpenWrite();
        }

        public Stream OpenRead(string path, CancellationToken token)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite(string path, CancellationToken token)
        {
            return new FileInfo(path).OpenWrite();
        }

        public void SetAttributes(string filePath, FileAttributes attr, CancellationToken token)
        {
            if (File.Exists(filePath))
                new FileInfo(filePath).Attributes = attr;
            else if (Directory.Exists(filePath)) new DirectoryInfo(filePath).Attributes = attr;
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName,
            IntPtr lpSecurityAttributes);

        private void DeleteDirectoryNonRecursive(DirectoryInfo targetDir, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!targetDir.Exists)
                return;
            targetDir.Attributes = FileAttributes.Normal;
            targetDir.Delete(false);
        }

        private void DeleteFile(FileInfo file, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!file.Exists)
                return;
            file.Attributes = FileAttributes.Normal;
            file.Delete();
        }

        private static void InternalCreateHardLink(string src, string tgt)
        {
            var res = CreateHardLink(tgt, src, IntPtr.Zero);
            if (!res)
            {
                var ex = new Win32Exception(Marshal.GetLastWin32Error());
                throw ex;
            }
        }

        public static IFileSystem Instance => _intance.Value;
        private static readonly Lazy<IFileSystem> _intance = new Lazy<IFileSystem>(() => new FileSystem(), true);
    }
}