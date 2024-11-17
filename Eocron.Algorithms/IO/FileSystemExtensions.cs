using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Eocron.Algorithms.IO;

public static class FileSystemExtensions
{
    public static async Task WriteAllTextAsync(this IFileSystem fs, string filePath, string content, CancellationToken ct = default, Encoding encoding = null)
    {
        await using var s = await fs.OpenFileAsync(filePath, FileMode.Create, ct).ConfigureAwait(false);
        await using var sw = new StreamWriter(s, encoding ?? DefaultEncoding);
        await sw.WriteAsync(content).ConfigureAwait(false);
    }
        
    public static async Task WriteAllBytesAsync(this IFileSystem fs, string filePath, ReadOnlyMemory<byte> content, CancellationToken ct = default)
    {
        await using var s = await fs.OpenFileAsync(filePath, FileMode.Create, ct).ConfigureAwait(false);
        await s.WriteAsync(content, ct).ConfigureAwait(false);
    }

    public static async Task<string> ReadAllTextAsync(this IFileSystem fs, string filePath,
        CancellationToken ct = default, Encoding encoding = null)
    {
        await using var s = await fs.OpenFileAsync(filePath, FileMode.Open, ct).ConfigureAwait(false);
        using var sr = new StreamReader(s, encoding ?? DefaultEncoding);
        return await sr.ReadToEndAsync(ct).ConfigureAwait(false);
    }
        
    public static async Task ReadAllBytesAsync(this IFileSystem fs, string filePath, byte[] destination,
        CancellationToken ct = default)
    {
        await using var s = await fs.OpenFileAsync(filePath, FileMode.Open, ct).ConfigureAwait(false);
        await using var ms = new MemoryStream(destination);
        await s.CopyToAsync(ms, ct).ConfigureAwait(false);
    }
        
    public static async Task<byte[]> ReadAllBytesAsync(this IFileSystem fs, string filePath,
        CancellationToken ct = default)
    {
        await using var s = await fs.OpenFileAsync(filePath, FileMode.Open, ct).ConfigureAwait(false);
        await using var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct).ConfigureAwait(false);
        return ms.ToArray();
    }

    public static async Task CopyFileToOtherFileSystemAsync(this IFileSystem sourceFileSystem, IFileSystem targetFileSystem,
        string sourceFilePath, string targetFilePath, CancellationToken ct = default)
    {
        await using var src = await sourceFileSystem.OpenFileAsync(sourceFilePath, FileMode.Open, ct).ConfigureAwait(false);
        await using var tgt = await targetFileSystem.OpenFileAsync(targetFilePath, FileMode.CreateNew, ct).ConfigureAwait(false);
        await src.CopyToAsync(tgt, ct).ConfigureAwait(false);
    }

    public static async Task CopyDirectoryToOtherFileSystemAsync(this IFileSystem sourceFileSystem,
        IFileSystem targetFileSystem,
        string sourceFolderPath, string targetFolderPath, CancellationToken ct = default)
    {
        if (!await sourceFileSystem.IsDirectoryExistAsync(sourceFolderPath, ct).ConfigureAwait(false))
        {
            throw new DirectoryNotFoundException(sourceFolderPath);
        }

        await targetFileSystem.TryCreateDirectoryAsync(targetFolderPath, ct).ConfigureAwait(false);
        await foreach (var dirBatch in sourceFileSystem.GetDirectoriesAsync(sourceFolderPath, "*", SearchOption.AllDirectories, ct).ConfigureAwait(false))
        {
            var renames = dirBatch.Select(x => new
            {
                src = x,
                tgt = ChangeRoot(sourceFolderPath, targetFolderPath, x)
            });
            await Parallel.ForEachAsync(renames, ct,
                    async (d, nct) => await targetFileSystem.TryCreateDirectoryAsync(d.tgt, nct).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
            
        await foreach (var fileBatch in sourceFileSystem.GetFilesAsync(sourceFolderPath, "*", SearchOption.AllDirectories, ct).ConfigureAwait(false))
        {
            var renames = fileBatch.Select(x => new
            {
                src = x,
                tgt = ChangeRoot(sourceFolderPath, targetFolderPath, x)
            });
            await Parallel.ForEachAsync(renames, ct,
                    async (d, nct) => await CopyFileToOtherFileSystemAsync(sourceFileSystem, targetFileSystem, d.src, d.tgt, nct).ConfigureAwait(false))
                .ConfigureAwait(false);
        }
    }

    private static string ChangeRoot(string sourceBasePath, string targetBasePath, string sourcePath)
    {
        return targetBasePath + sourcePath.Substring(sourceBasePath.Length);
    }

    public static Encoding DefaultEncoding = Encoding.UTF8;
}