using System;

namespace Eocron.Algorithms.Sorted.IndexSelectors
{
    public interface IIndexSelector
    {
        /// <summary>
        /// Retrieves most probable position of value in collection, based on its offset/count in collection.
        /// </summary>
        /// <param name="range">Collection range in which search is performed</param>
        /// <returns>Next most probable position, in case of binary search it is simply middle of the range</returns>
        int GetNextMiddle(Range range);
    }
}