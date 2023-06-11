
//using ZetaLongPaths;

namespace Eocron.Algorithms.FileCache
{
    //public class ZlpFileSystem : IFileSystem
    //{
    //    public virtual Task<bool> FileExistAsync(string path, CancellationToken token)
    //    {
    //        return Task.FromResult(new ZlpFileInfo(path).Exists);
    //    }
    //    public virtual Task<bool> DirectoryExistAsync(string path, CancellationToken token)
    //    {
    //        return Task.FromResult(new ZlpDirectoryInfo(path).Exists);
    //    }

    //    public virtual Task MoveAsync(string src, string tgt, CancellationToken token)
    //    {
    //        var file = new ZlpFileInfo(src);
    //        var dir = new ZlpDirectoryInfo(src);

    //        if (file.Exists)
    //        {
    //            file.MoveTo(tgt);
    //        }
    //        else if (dir.Exists)
    //        {
    //            dir.MoveTo(tgt);
    //        }
    //        return Task.CompletedTask;
    //    }

    //    public Task CopyAsync(string src, string tgt, CancellationToken token, bool hardLinkIfPossible)
    //    {
    //        var srcInfo = new ZlpFileInfo(src);
    //        var tgtInfo = new ZlpFileInfo(tgt);


    //        if (hardLinkIfPossible)
    //        {
    //            //hard links possible only on same drive
    //            var possible = ZlpPathHelper.GetPathRoot(srcInfo.FullName) ==
    //                           ZlpPathHelper.GetPathRoot(tgtInfo.FullName);
    //            if (possible)
    //            {
    //                CreateHardLink(src, tgt);
    //                return Task.CompletedTask;
    //            }
    //        }
            
    //        srcInfo.CopyTo(tgtInfo, false);
    //        return Task.CompletedTask;
    //    }

    //    [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    //    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    //    public static void CreateHardLink(string src, string tgt)
    //    {
    //        var res = CreateHardLink(tgt, src, IntPtr.Zero);
    //        if (!res)
    //        {
    //            var ex = new Win32Exception(Marshal.GetLastWin32Error());
    //            throw ex;
    //        }
    //    }

    //    public Task<Stream> OpenReadAsync(string path, CancellationToken token)
    //    {
    //        return Task.FromResult((Stream)new ZlpFileInfo(path).OpenRead());
    //    }

    //    public Task<Stream> OpenCreateAsync(string path, CancellationToken token)
    //    {
    //        return Task.FromResult((Stream)new ZlpFileInfo(path).OpenCreate());
    //    }

    //    public Task<Stream> OpenWriteAsync(string path, CancellationToken token)
    //    {
    //        return Task.FromResult((Stream)new ZlpFileInfo(path).OpenWrite());
    //    }

    //    public virtual async Task DeleteFileAsync(string path, CancellationToken token)
    //    {
    //        await DeleteFileAsync(new ZlpFileInfo(path), token);
    //    }
    //    public virtual async Task DeleteDirectoryNonRecursiveAsync(string path, CancellationToken token)
    //    {
    //        await DeleteDirectoryNonRecursiveAsync(new ZlpDirectoryInfo(path), token);
    //    }

    //    public virtual Task<string[]> GetFilesAsync(string path, string pattern, SearchOption option, CancellationToken token)
    //    {
    //        return Task.FromResult(new ZlpDirectoryInfo(path).GetFiles(pattern, option).Select(x => x.FullName).ToArray());
    //    }

    //    public virtual Task<string[]> GetDirectoriesAsync(string path, string pattern, SearchOption option, CancellationToken token)
    //    {
    //        return Task.FromResult(new ZlpDirectoryInfo(path).GetDirectories(pattern, option).Select(x => x.FullName).ToArray());
    //    }

    //    public Task CreateDirectoryAsync(string path, CancellationToken token)
    //    {
    //        new ZlpDirectoryInfo(path).Create();
    //        return Task.CompletedTask;
    //    }

    //    private Task DeleteFileAsync(ZlpFileInfo file, CancellationToken token)
    //    {
    //        token.ThrowIfCancellationRequested();
    //        if (!file.Exists)
    //            return Task.CompletedTask;
    //        file.Attributes = ZetaLongPaths.Native.FileAttributes.Normal;
    //        file.Delete();
    //        return Task.CompletedTask;
    //    }
    //    private Task DeleteDirectoryNonRecursiveAsync(ZlpDirectoryInfo targetDir, CancellationToken token)
    //    {
    //        token.ThrowIfCancellationRequested();
    //        if (!targetDir.Exists)
    //            return Task.CompletedTask;
    //        targetDir.Attributes = ZetaLongPaths.Native.FileAttributes.Normal;
    //        targetDir.Delete(false);
    //        return Task.CompletedTask;
    //    }

    //    public Task<bool> EqualsAsync(string firstPath, string secondPath, CancellationToken token)
    //    {
    //        return Task.FromResult(new ZlpFileInfo(firstPath).EqualsNoCase(new ZlpFileInfo(secondPath)));
    //    }
    //}
}
