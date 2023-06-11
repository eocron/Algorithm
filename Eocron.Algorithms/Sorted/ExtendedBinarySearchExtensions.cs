﻿using System;
using System.Collections.Generic;
using Eocron.Algorithms.Sorted.IndexSelectors;

namespace Eocron.Algorithms.Sorted
{
    public static class ExtendedBinarySearchExtensions
    {
        /// <summary>
        ///     Use binary search approach to find index in sorted list.
        ///     Asymptotic worst case: O(log(n))
        ///     Memory asymptotic worst case: O(1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <param name="descendingOrder">True if array sorted in descending order.</param>
        /// <param name="indexSelectorBuilder">Creates index selector, which evaluates to next middle at each step of binary search.</param>
        /// <returns>Returns index of element if found and -1 otherwise.</returns>
        public static int ExtendedBinarySearchIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null,
            bool descendingOrder = false, IIndexSelectorBuilder<T> indexSelectorBuilder = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return -1;

            comparer = comparer ?? Comparer<T>.Default;
            var indexSelector = (indexSelectorBuilder ?? LogarithmicIndexSelectorBuilder<T>.Default).Build(collection, value, comparer);
            
            var lower = 0;
            var upper = collection.Count - 1;

            while (lower <= upper)
            {
                var middle = indexSelector.GetNextMiddle(new Range(lower, upper));
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (descendingOrder)
                    comparisonResult = -comparisonResult;

                if (comparisonResult == 0)
                    return middle;
                if (comparisonResult < 0)
                    upper = middle - 1;
                else
                    lower = middle + 1;
            }

            return -1;
        }

        /// <summary>
        ///     Use binary search approach to find lower bound index in sorted list.
        ///     Asymptotic worst case: O(log(n))
        ///     Memory asymptotic worst case: O(1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <param name="descendingOrder">True if array sorted in descending order.</param>
        /// <param name="indexSelectorBuilder">Creates index selector, which evaluates to next middle at each step of binary search.</param>
        /// <returns>
        ///     Index of last element which is not equal and lower than value. Returns (-1) if value is lower than first value
        ///     and (Count-1) if it is greater than last value.
        /// </returns>
        public static int ExtendedLowerBoundIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null,
            bool descendingOrder = false, IIndexSelectorBuilder<T> indexSelectorBuilder = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return -1;

            comparer = comparer ?? Comparer<T>.Default;
            var indexSelector = (indexSelectorBuilder ?? LogarithmicIndexSelectorBuilder<T>.Default).Build(collection, value, comparer);
            
            var lower = 0;
            var upper = collection.Count;
            while (lower < upper)
            {
                var middle = indexSelector.GetNextMiddle(new Range(lower, upper));
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (descendingOrder)
                    comparisonResult = -comparisonResult;

                if (comparisonResult <= 0)
                    upper = middle;
                else
                    lower = middle + 1;
            }

            return lower - 1;
        }

        /// <summary>
        ///     Use binary search approach to find upper bound index in sorted list.
        ///     Asymptotic worst case: O(log(n))
        ///     Memory asymptotic worst case: O(1)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Sorted collection.</param>
        /// <param name="value">Value to search.</param>
        /// <param name="comparer">Comparer to compare values.</param>
        /// <param name="descendingOrder">True if array sorted in descending order.</param>
        /// <param name="indexSelectorBuilder">Creates index selector, which evaluates to next middle at each step of binary search.</param>
        /// <returns>
        ///     Index of first element which is not equal and greater than value. Returns (Count) if value is greater than
        ///     last value and (0) if it is lower than first value.
        /// </returns>
        public static int ExtendedUpperBoundIndexOf<T>(this IList<T> collection, T value, IComparer<T> comparer = null,
            bool descendingOrder = false, IIndexSelectorBuilder<T> indexSelectorBuilder = null)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));
            if (collection.Count == 0)
                return 0;

            comparer = comparer ?? Comparer<T>.Default;
            var indexSelector = (indexSelectorBuilder ?? LogarithmicIndexSelectorBuilder<T>.Default).Build(collection, value, comparer);

            var lower = 0;
            var upper = collection.Count;
            while (lower < upper)
            {
                var middle = indexSelector.GetNextMiddle(new Range(lower, upper));
                var comparisonResult = comparer.Compare(value, collection[middle]);
                if (descendingOrder)
                    comparisonResult = -comparisonResult;

                if (comparisonResult >= 0)
                    lower = middle + 1;
                else
                    upper = middle;
            }

            return lower;
        }
    }
}