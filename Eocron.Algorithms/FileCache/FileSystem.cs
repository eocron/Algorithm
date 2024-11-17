using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.FileCache
{
    public sealed class FileSystem : IFileSystem, IDisposable, IAsyncDisposable
    {
        public FileSystem(
            string baseFolder = "",
            FileSystemFeature features = FileSystemFeature.CreateBaseDirectoryIfNotExists,
            MemoryPool<byte> pool = null,
            int? maxDegreeOfParallelism = null)
        {
            maxDegreeOfParallelism ??= Environment.ProcessorCount * 2;
            baseFolder = Path.GetFullPath(baseFolder ?? "").Trim(Path.PathSeparator, Path.AltDirectorySeparatorChar);
            
            if (!features.HasFlag(FileSystemFeature.CreateBaseDirectoryIfNotExists) && !Directory.Exists(baseFolder))
                throw new DirectoryNotFoundException(baseFolder);
            if (maxDegreeOfParallelism <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDegreeOfParallelism));
            
            _baseFolder = baseFolder;
            _features = features;
            _pool = pool ?? MemoryPool<byte>.Shared;
            _maxDegreeOfParallelism = maxDegreeOfParallelism.Value;
        }

        public async Task CopyFileAsync(string sourceFilePath, string targetFilePath, CancellationToken ct = default)
        {
            var srcInfo = await GetPhysicalFile(sourceFilePath, ct).ConfigureAwait(false);
            var tgtInfo = await GetPhysicalFile(targetFilePath, ct).ConfigureAwait(false);

            srcInfo.CopyTo(tgtInfo.FullName, false);
            await SetFileAttributesAsync(targetFilePath, FileAttributes.Normal, ct).ConfigureAwait(false);
        }

        public async Task CreateFileHardLinkAsync(string sourceFilePath, string targetFilePath,
            CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _sync?.Dispose();
            if (_features.HasFlag(FileSystemFeature.DeleteBaseDirectoryOnDispose))
                TryDeleteDirectoryAsync(string.Empty, CancellationToken.None).Wait();
        }

        public async ValueTask DisposeAsync()
        {
            await SafeDisposeAsync(_sync).ConfigureAwait(false);
            if (_features.HasFlag(FileSystemFeature.DeleteBaseDirectoryOnDispose))
                await TryDeleteDirectoryAsync(string.Empty, CancellationToken.None).ConfigureAwait(false);
        }

        public async IAsyncEnumerable<string[]> GetDirectoriesAsync(string folderPath, string pattern,
            SearchOption option,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var dir = await GetPhysicalDirectory(folderPath, ct).ConfigureAwait(false);
            yield return await Task
                .WhenAll(dir.GetDirectories(pattern, option).Select(async x => await GetVirtualPath(x.FullName, ct)))
                .ConfigureAwait(false);
        }

        public async Task<FileAttributes> GetFileAttributesAsync(string filePath, CancellationToken ct = default)
        {
            return (await GetPhysicalFile(filePath, ct).ConfigureAwait(false)).Attributes;
        }

        public async IAsyncEnumerable<string[]> GetFilesAsync(string folderPath, string pattern, SearchOption option,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var dir = await GetPhysicalDirectory(folderPath, ct).ConfigureAwait(false);
            yield return await Task
                .WhenAll(dir.GetFiles(pattern, option).Select(async x => await GetVirtualPath(x.FullName, ct)))
                .ConfigureAwait(false);
        }

        public async Task<bool> IsDirectoryExistAsync(string folderPath, CancellationToken ct = default)
        {
            return (await GetPhysicalDirectory(folderPath, ct).ConfigureAwait(false)).Exists;
        }

        public async Task<bool> IsFileExistAsync(string filePath, CancellationToken ct = default)
        {
            return (await GetPhysicalFile(filePath, ct).ConfigureAwait(false)).Exists;
        }

        public async Task MoveDirectoryAsync(string sourceFolderPath, string targetFolderPath,
            CancellationToken ct = default)
        {
            var src = await GetPhysicalDirectory(sourceFolderPath, ct).ConfigureAwait(false);
            var tgt = await GetPhysicalDirectory(targetFolderPath, ct).ConfigureAwait(false);
            src.MoveTo(tgt.FullName);
        }

        public async Task MoveFileAsync(string sourceFilePath, string targetFilePath, CancellationToken ct = default)
        {
            var src = await GetPhysicalFile(sourceFilePath, ct).ConfigureAwait(false);
            var tgt = await GetPhysicalFile(targetFilePath, ct).ConfigureAwait(false);
            src.MoveTo(tgt.FullName);
        }

        public async Task<Stream> OpenFileAsync(string filePath, FileMode mode, CancellationToken ct = default)
        {
            var file = await GetPhysicalFile(filePath, ct).ConfigureAwait(false);
            return file.Open(mode);
        }

        public async Task SetFileAttributesAsync(string filePath, FileAttributes attributes,
            CancellationToken ct = default)
        {
            var file = await GetPhysicalFile(filePath, ct).ConfigureAwait(false);
            file.Attributes = attributes;
        }

        public async Task<bool> TryCreateDirectoryAsync(string folderPath, CancellationToken ct = default)
        {
            var dir = await GetPhysicalDirectory(folderPath, ct).ConfigureAwait(false);
            if (dir.Exists) return false;
            dir.Create();
            return true;
        }

        public async Task<bool> TryDeleteDirectoryAsync(string folderPath, CancellationToken ct = default)
        {
            var dir = await GetPhysicalDirectory(folderPath, ct).ConfigureAwait(false);
            if (!dir.Exists) return false;

            await TryFillWithJunkAsync(dir, ct).ConfigureAwait(false);
            dir.Delete(true);
            return true;
        }

        public async Task<bool> TryDeleteFileAsync(string filePath, CancellationToken ct = default)
        {
            var file = await GetPhysicalFile(filePath, ct).ConfigureAwait(false);
            if (!file.Exists) return false;
            await TryFillWithJunkAsync(file, ct).ConfigureAwait(false);
            file.Delete();
            return true;
        }

        private async Task<string> GetBaseDirectory(CancellationToken ct)
        {
            if (!_initialized)
            {
                await _sync.WaitAsync(ct).ConfigureAwait(false);
                try
                {
                    if (!_initialized)
                    {
                        if (_features.HasFlag(FileSystemFeature.CreateBaseDirectoryIfNotExists) &&
                            !Directory.Exists(_baseFolder)) Directory.CreateDirectory(_baseFolder);
                        await ValidateReadWriteAccessAsync(_baseFolder, ct).ConfigureAwait(false);
                        _initialized = true;
                    }
                }
                finally
                {
                    _sync.Release();
                }
            }

            return _baseFolder;
        }

        private async Task<DirectoryInfo> GetPhysicalDirectory(string virtualPath, CancellationToken ct)
        {
            return new DirectoryInfo(await GetPhysicalPath(virtualPath, ct).ConfigureAwait(false));
        }

        private async Task<FileInfo> GetPhysicalFile(string virtualPath, CancellationToken ct)
        {
            return new FileInfo(await GetPhysicalPath(virtualPath, ct).ConfigureAwait(false));
        }

        private async Task<string> GetPhysicalPath(string virtualPath, CancellationToken ct)
        {
            var baseFolder = await GetBaseDirectory(ct).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(virtualPath) ? baseFolder : Path.Combine(baseFolder, virtualPath);
        }

        private async Task<string> GetVirtualPath(string physicalPath, CancellationToken ct)
        {
            var baseFolder = await GetBaseDirectory(ct).ConfigureAwait(false);
            physicalPath = Path.GetFullPath(physicalPath);
            if (!physicalPath.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase))
                throw new AccessViolationException($"Base folder is {baseFolder}, but tried to access {physicalPath}");

            return physicalPath
                .Substring(0, baseFolder.Length)
                .Trim(Path.PathSeparator, Path.AltDirectorySeparatorChar)
                .Replace(Path.PathSeparator, Path.AltDirectorySeparatorChar);
        }

        private async ValueTask SafeDisposeAsync(object obj)
        {
            if (obj == null)
                return;

            switch (obj)
            {
                case IAsyncDisposable ad:
                    await ad.DisposeAsync().ConfigureAwait(false);
                    return;
                case IDisposable d:
                    d.Dispose();
                    return;
            }
        }

        private async Task TryFillWithJunkAsync(DirectoryInfo dir, CancellationToken ct)
        {
            if (!_features.HasFlag(FileSystemFeature.FillDeletedFilesWithJunk)) return;

            var files = dir.GetFiles("*", SearchOption.AllDirectories);
            if (!files.Any()) return;

            await Parallel.ForEachAsync(files,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = _maxDegreeOfParallelism,
                    CancellationToken = ct
                },
                async (file, nct) =>
                {
                    await TryFillWithJunkAsync(file, nct).ConfigureAwait(false);
                    file.Delete();
                }).ConfigureAwait(false);
        }

        private async Task TryFillWithJunkAsync(FileInfo file, CancellationToken ct)
        {
            if (!_features.HasFlag(FileSystemFeature.FillDeletedFilesWithJunk)) return;

            const int bufferSize = 1 << 16;
            var fileSize = file.Length;
            var buff = _pool.Rent(bufferSize);
            try
            {
                await using var fs = file.OpenWrite();
                while (fileSize > 0)
                {
                    CryptoRandom.GetNonZeroBytes(buff.Memory.Span);
                    var sliced = buff.Memory.Slice(0, (int)Math.Min(fileSize, buff.Memory.Length));
                    await fs.WriteAsync(sliced, ct).ConfigureAwait(false);
                    fileSize -= buff.Memory.Length;
                }
            }
            finally
            {
                buff.Dispose();
            }
        }

        private static async Task ValidateReadWriteAccessAsync(string folder, CancellationToken ct)
        {
            var tmpFile = Path.Combine(folder, Guid.NewGuid().ToString("N"));
            await File.WriteAllTextAsync(tmpFile, nameof(ValidateReadWriteAccessAsync), ct).ConfigureAwait(false);
            try
            {
                await File.ReadAllTextAsync(tmpFile, ct).ConfigureAwait(false);
            }
            finally
            {
                File.Delete(tmpFile);
            }
        }

        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();
        private readonly FileSystemFeature _features;
        private readonly int _maxDegreeOfParallelism;
        private readonly MemoryPool<byte> _pool;
        private readonly SemaphoreSlim _sync = new(1);
        private readonly string _baseFolder;

        private volatile bool _initialized;
    }
}