using System;
using System.IO;
using System.Threading;

namespace Algorithm.FileCache
{
    public interface IFileCache<TKey>
    {
        void Invalidate(CancellationToken token);

        void Invalidate(TKey key, CancellationToken token);

        void GarbageCollect(CancellationToken token);

        Stream GetStreamOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token, ICacheExpirationPolicy policy);
        Stream GetStreamOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token, ICacheExpirationPolicy policy);
        void AddOrUpdateStream(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy, bool leaveOpen = false);

        void AddOrUpdateFile(TKey key, string sourceFilePath, CancellationToken token, ICacheExpirationPolicy policy);

        void GetFileOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy);
        void GetFileOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token, string targetFilePath, ICacheExpirationPolicy policy);

        bool TryGetFile(TKey key, CancellationToken token, string targetFilePath);

        Stream TryGetStream(TKey key, CancellationToken token);
    }
}
