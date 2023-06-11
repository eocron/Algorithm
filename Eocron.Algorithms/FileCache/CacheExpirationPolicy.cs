using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Eocron.Algorithms.FileCache
{
    internal sealed class AbsoluteExpirationPolicy : ICacheExpirationPolicy
    {
        public AbsoluteExpirationPolicy(DateTime expireIn)
        {
            _expireIn = expireIn;
        }

        public bool IsExpired(DateTime now)
        {
            return _expireIn <= now;
        }

        public void LogAccess(DateTime now)
        {
            //no reaction to access
        }

        public bool TryMerge(ICacheExpirationPolicy toMerge)
        {
            var obj = toMerge as AbsoluteExpirationPolicy;
            if (obj == null)
                return false;
            _expireIn = obj._expireIn;
            return true;
        }

        private DateTime _expireIn;
    }

    internal sealed class SlidingExpirationPolicy : ICacheExpirationPolicy
    {
        public SlidingExpirationPolicy(DateTime now, TimeSpan slide)
        {
            _expireIn = now + slide;
            _toUpdate = slide;
        }

        public bool IsExpired(DateTime now)
        {
            return _expireIn <= now;
        }

        public void LogAccess(DateTime now)
        {
            if (IsExpired(now))
                return;

            _expireIn = now + _toUpdate;
        }

        public bool TryMerge(ICacheExpirationPolicy toMerge)
        {
            var obj = toMerge as SlidingExpirationPolicy;
            if (obj == null)
                return false;
            _toUpdate = obj._toUpdate;
            return true;
        }

        private DateTime _expireIn;

        private TimeSpan _toUpdate;
    }

    /// <summary>
    ///     Represent policy in which it expired only if one of sub policies expired.
    /// </summary>
    internal class AnyExpirationPolicy : ICacheExpirationPolicy
    {
        public bool IsExpired(DateTime now)
        {
            return _policies?.Any(x => x.IsExpired(now)) ?? false;
        }

        public void LogAccess(DateTime now)
        {
            foreach (var fileCacheExpirationPolicy in _policies) fileCacheExpirationPolicy.LogAccess(now);
        }

        public bool TryMerge(ICacheExpirationPolicy toMerge)
        {
            if (toMerge == null)
                return true;

            foreach (var fileCacheExpirationPolicy in _policies)
                if (fileCacheExpirationPolicy.TryMerge(toMerge))
                    return true;
            _policies.Add(toMerge);
            return true;
        }

        private readonly ConcurrentBag<ICacheExpirationPolicy> _policies = new ConcurrentBag<ICacheExpirationPolicy>();
    }

    public static class CacheExpirationPolicy
    {
        /// <summary>
        ///     It will expire in specified datetime.
        /// </summary>
        public static ICacheExpirationPolicy AbsoluteUtc(DateTime expireIn)
        {
            return new AbsoluteExpirationPolicy(expireIn);
        }


        /// <summary>
        ///     Updateable expiration policy. It expires only if slide timeout reached and no access to object was registered. I.e.
        ///     expiration because of unpopularity
        /// </summary>
        public static ICacheExpirationPolicy SlidingUtc(TimeSpan slide)
        {
            return new SlidingExpirationPolicy(DateTime.UtcNow, slide);
        }

        /// <summary>
        ///     Updates policy and registers access.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="policy"></param>
        /// <param name="otherPolicy"></param>
        /// <returns></returns>
        internal static T Pulse<T>(this T policy, ICacheExpirationPolicy otherPolicy) where T : ICacheExpirationPolicy
        {
            policy.TryMerge(otherPolicy);
            policy.LogAccess(DateTime.UtcNow);
            return policy;
        }
    }
}