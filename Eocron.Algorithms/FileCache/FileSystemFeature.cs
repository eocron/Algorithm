using System;

namespace Eocron.Algorithms.FileCache
{
    [Flags]
    public enum FileSystemFeature
    {
        None = 0,
        Default = CreateBaseDirectoryIfNotExists,
        /// <summary>
        /// Temporal file system, created on use, vanishes on dispose.
        /// </summary>
        Temporal = CreateBaseDirectoryIfNotExists | DeleteBaseDirectoryOnDispose,
        /// <summary>
        /// Fills files with junk before deletion, ensures non-recoverability through special software.
        /// </summary>
        FillDeletedFilesWithJunk = 1<<0,
        /// <summary>
        /// Creates base directory if not exists.
        /// </summary>
        CreateBaseDirectoryIfNotExists = 1<<1,
        /// <summary>
        /// Deletes base directory on dispose.
        /// </summary>
        DeleteBaseDirectoryOnDispose = 1<<2
    }
}