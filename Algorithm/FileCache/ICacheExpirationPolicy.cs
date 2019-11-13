using System;

namespace Algorithm.FileCache
{
    /// <summary>
    /// Expiration policy of file. Allows one to invalidate object by custom behavior and constant updates.
    /// </summary>
    public interface ICacheExpirationPolicy
    {
        bool IsExpired(DateTime now);

        void LogAccess(DateTime now);

        bool TryMerge(ICacheExpirationPolicy toMerge);
    }
}
