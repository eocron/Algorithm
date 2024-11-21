using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.IO;

public sealed class FileSystem : IFileSystem, IExposedFileSystem, IDisposable, IAsyncDisposable
{
    public FileSystem(
        string baseFolder = "",
        FileSystemFeature features = FileSystemFeature.CreateBaseDirectoryIfNotExists,
        MemoryPool<byte> pool = null,
        int? maxDegreeOfParallelism = null)
    {
        maxDegreeOfParallelism ??= Environment.ProcessorCount * 2;
        baseFolder = Path.GetFullPath(baseFolder ?? "").TrimEnd(Path.PathSeparator, Path.AltDirectorySeparatorChar);
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
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var srcInfo = GetPhysicalFile(sourceFilePath);
        var tgtInfo = GetPhysicalFile(targetFilePath);

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
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var dir = GetPhysicalDirectory(folderPath);
        yield return dir.GetDirectories(pattern, option).Select( x => GetVirtualPath(x.FullName)).ToArray();
    }

    public async Task<FileAttributes> GetFileAttributesAsync(string filePath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        return GetPhysicalFile(filePath).Attributes;
    }

    public async IAsyncEnumerable<string[]> GetFilesAsync(string folderPath, string pattern, SearchOption option,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var dir = GetPhysicalDirectory(folderPath);
        yield return dir.GetFiles(pattern, option).Select( x => GetVirtualPath(x.FullName)).ToArray();
    }

    public async Task<bool> IsDirectoryExistAsync(string folderPath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        return GetPhysicalDirectory(folderPath).Exists;
    }

    public async Task<bool> IsFileExistAsync(string filePath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        return GetPhysicalFile(filePath).Exists;
    }

    public async Task MoveDirectoryAsync(string sourceFolderPath, string targetFolderPath,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var src = GetPhysicalDirectory(sourceFolderPath);
        var tgt = GetPhysicalDirectory(targetFolderPath);
        src.MoveTo(tgt.FullName);
    }

    public async Task MoveFileAsync(string sourceFilePath, string targetFilePath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var src = GetPhysicalFile(sourceFilePath);
        var tgt = GetPhysicalFile(targetFilePath);
        src.MoveTo(tgt.FullName);
    }

    public async Task<Stream> OpenFileAsync(string filePath, FileMode mode, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var file = GetPhysicalFile(filePath);
        return file.Open(mode);
    }

    public async Task SetFileAttributesAsync(string filePath, FileAttributes attributes,
        CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var file = GetPhysicalFile(filePath);
        file.Attributes = attributes;
    }

    public async Task<bool> TryCreateDirectoryAsync(string folderPath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var dir = GetPhysicalDirectory(folderPath);
        if (dir.Exists) return false;
        dir.Create();
        return true;
    }

    public async Task<bool> TryDeleteDirectoryAsync(string folderPath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var dir = GetPhysicalDirectory(folderPath);
        if (!dir.Exists) return false;

        await TryFillWithJunkAsync(dir, ct).ConfigureAwait(false);
        dir.Delete(true);
        return true;
    }

    public async Task<bool> TryDeleteFileAsync(string filePath, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);
        var file = GetPhysicalFile(filePath);
        if (!file.Exists) return false;
        await TryFillWithJunkAsync(file, ct).ConfigureAwait(false);
        file.Delete();
        return true;
    }
        
    public DirectoryInfo GetPhysicalDirectory(string virtualPath)
    {
        return new DirectoryInfo(GetPhysicalPath(virtualPath));
    }

    public FileInfo GetPhysicalFile(string virtualPath)
    {
        return new FileInfo(GetPhysicalPath(virtualPath));
    }

    private string GetBaseDirectory()
    {
        return _baseFolder;
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized)
            return;
        
        await _sync.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized)
                return;

            await InitializeAsync(ct).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _sync.Release();
        }
    }

    private async Task InitializeAsync(CancellationToken ct)
    {
        var dir = new DirectoryInfo(_baseFolder);
        if (_features.HasFlag(FileSystemFeature.CreateBaseDirectoryIfNotExists) && !dir.Exists)
        {
            dir.Create();
        }
        await ValidateReadWriteAccessAsync(_baseFolder, ct).ConfigureAwait(false);
    }

    private string GetPhysicalPath(string virtualPath)
    {
        var baseFolder = GetBaseDirectory();
        return string.IsNullOrWhiteSpace(virtualPath) ? baseFolder : Path.Combine(baseFolder, virtualPath);
    }

    private string GetVirtualPath(string physicalPath)
    {
        var baseFolder = GetBaseDirectory();
        physicalPath = Path.GetFullPath(physicalPath);
        if (!physicalPath.StartsWith(baseFolder, StringComparison.OrdinalIgnoreCase))
            throw new AccessViolationException($"Base folder is {baseFolder}, but tried to access {physicalPath}");

        return physicalPath
            .Substring(0, baseFolder.Length)
            .TrimEnd(Path.PathSeparator, Path.AltDirectorySeparatorChar)
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
        
    public string BaseFolder => _baseFolder;
    public FileSystemFeature Features => _features;

    private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();
    private readonly FileSystemFeature _features;
    private readonly int _maxDegreeOfParallelism;
    private readonly MemoryPool<byte> _pool;
    private readonly SemaphoreSlim _sync = new(1);
    private readonly string _baseFolder;

    private volatile bool _initialized;
}