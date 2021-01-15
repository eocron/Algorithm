using System;
using System.Collections.Generic;

namespace Algorithm.Sorted
{
    public static class BinarySearchExtensions
    {
        /// <summary>
        /// Use binary search approach to find index in sorted list.
        /// Asymptotic worst case: O(log(n))
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <returns>Returns index of element if found and -1 otherwise.</returns>
        public static int BinarySearchIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return -1;

            comparer = comparer ?? Comparer<T>.Default;

            var lower = 0;
            var upper = collection.Count - 1;

            while (lower <= upper)
            {
                var middle = lower + ((upper - lower) >> 1);
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (comparisonResult == 0)
                    return middle;
                else if (comparisonResult < 0)
                    upper = middle - 1;
                else
                    lower = middle + 1;
            }

            return -1;
        }

        /// <summary>
        /// Use binary search approach to find lower bound index in sorted list.
        /// Asymptotic worst case: O(log(n))
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <returns>Index of last element which is not equal and lower than value. Returns (-1) if value is lower than first value and (Count-1) if it is greater than last value.</returns>
        public static int LowerBoundIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return -1;

            comparer = comparer ?? Comparer<T>.Default;

            var lower = 0;
            var upper = collection.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) >> 1);
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (comparisonResult <= 0)
                {
                    upper = middle;
                }
                else
                {
                    lower = middle + 1;
                }
            }
            return lower-1;
        }

        /// <summary>
        /// Use binary search approach to find upper bound index in sorted list.
        /// Asymptotic worst case: O(log(n))
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <returns>Index of first element which is not equal and greater than value. Returns (Count) if value is greater than last value and (0) if it is lower than first value.</returns>
        public static int UpperBoundIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return 0;

            comparer = comparer ?? Comparer<T>.Default;

            var lower = 0;
            var upper = collection.Count;
            while (lower < upper)
            {
                var middle = lower + ((upper - lower) >> 1);
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (comparisonResult >= 0)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle;
                }
            }
            return lower;
        }
    }
}
