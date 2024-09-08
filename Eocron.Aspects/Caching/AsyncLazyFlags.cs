using System;

namespace Eocron.Aspects.Caching
{
    /// <summary>
    /// Flags controlling the behavior of <see cref="AsyncLazy{T}"/>.
    /// </summary>
    [Flags]
    public enum AsyncLazyFlags
    {
        /// <summary>
        /// No special flags. The factory method is executed on a thread pool thread, and does not retry initialization on failures (failures are cached).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Execute the factory method on the calling thread.
        /// </summary>
        ExecuteOnCallingThread = 0x1,

        /// <summary>
        /// If the factory method fails, then re-run the factory method the next time instead of caching the failed task.
        /// </summary>
        RetryOnFailure = 0x2,
    }
}