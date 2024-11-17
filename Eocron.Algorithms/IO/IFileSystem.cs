using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.IO;

public interface IFileSystem
{
    Task CopyFileAsync(string sourcePath, string targetPath, CancellationToken ct = default);
    Task CreateFileHardLinkAsync(string sourceFilePath, string targetFilePath, CancellationToken ct = default);
    Task<bool> TryDeleteFileAsync(string filePath, CancellationToken ct = default);
    Task<bool> IsFileExistAsync(string filePath, CancellationToken ct = default);
    Task MoveFileAsync(string sourceFilePath, string targetFilePath, CancellationToken ct = default);
    Task<Stream> OpenFileAsync(string filePath, FileMode mode, CancellationToken ct = default);
    Task SetFileAttributesAsync(string filePath, FileAttributes attributes, CancellationToken ct = default);
    Task<FileAttributes> GetFileAttributesAsync(string filePath, CancellationToken ct = default);
        
    Task<bool> TryCreateDirectoryAsync(string folderPath, CancellationToken ct = default);
    Task<bool> TryDeleteDirectoryAsync(string folderPath, CancellationToken ct = default);
    Task<bool> IsDirectoryExistAsync(string folderPath, CancellationToken ct = default);
    IAsyncEnumerable<string[]> GetDirectoriesAsync(string folderPath, string pattern, SearchOption option, CancellationToken ct = default);
    IAsyncEnumerable<string[]> GetFilesAsync(string folderPath, string pattern, SearchOption option, CancellationToken ct = default);
    Task MoveDirectoryAsync(string sourceFolderPath, string targetFolderPath, CancellationToken ct = default);
}