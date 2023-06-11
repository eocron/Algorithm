using System;
using System.IO;
using System.Threading;

namespace Eocron.Algorithms.FileCache
{
    public interface IFileCache<TKey>
    {
        void AddOrUpdateFile(TKey key, string sourceFilePath, CancellationToken token, ICacheExpirationPolicy policy);

        void AddOrUpdateStream(TKey key, Stream stream, CancellationToken token, ICacheExpirationPolicy policy,
            bool leaveOpen = false);

        void GarbageCollect(CancellationToken token);

        void GetFileOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token, string targetFilePath,
            ICacheExpirationPolicy policy);

        void GetFileOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token, string targetFilePath,
            ICacheExpirationPolicy policy);

        Stream GetStreamOrAddFile(TKey key, Func<TKey, string> provider, CancellationToken token,
            ICacheExpirationPolicy policy);

        Stream GetStreamOrAddStream(TKey key, Func<TKey, Stream> provider, CancellationToken token,
            ICacheExpirationPolicy policy);

        void Invalidate(CancellationToken token);

        void Invalidate(TKey key, CancellationToken token);

        bool TryGetFile(TKey key, CancellationToken token, string targetFilePath);

        Stream TryGetStream(TKey key, CancellationToken token);
    }
}