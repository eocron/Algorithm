using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Tree
{
    public enum TraversalKind
    {
        /// <summary>
        ///     RNL
        /// </summary>
        ReverseInOrder,

        /// <summary>
        ///     NLR
        /// </summary>
        PreOrder,

        /// <summary>
        ///     LRN
        /// </summary>
        PostOrder,

        /// <summary>
        ///     BFS
        /// </summary>
        LevelOrder
    }

    /// <summary>
    ///     https://en.wikipedia.org/wiki/Tree_traversal
    /// </summary>
    public static class TreeTraversalExtensions
    {
        /// <summary>
        ///     Perform traversal of basic Tree-like structures or Graphs without cycles.
        ///     Asymptotic worst case: O(n)
        ///     Memory asymptotic worst case: O(n)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root">Starting subtree root.</param>
        /// <param name="childrenProvider">Children provider for each node.</param>
        /// <param name="kind">Traversal algorithm to use.</param>
        /// <returns></returns>
        public static IEnumerable<T> Traverse<T>(this T root, Func<T, IEnumerable<T>> childrenProvider,
            TraversalKind kind = default)
        {
            if (childrenProvider == null)
                throw new ArgumentNullException(nameof(childrenProvider));
            switch (kind)
            {
                case TraversalKind.ReverseInOrder:
                    return root.TraverseReverseInOrder(childrenProvider);
                case TraversalKind.PreOrder:
                    return root.TraversePreOrder(childrenProvider);
                case TraversalKind.PostOrder:
                    return root.TraversePostOrder(childrenProvider);
                case TraversalKind.LevelOrder:
                    return root.TraverseLevelOrder(childrenProvider);
            }

            throw new NotImplementedException(kind.ToString());
        }

        /// <summary>
        ///     BFS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        private static IEnumerable<T> TraverseLevelOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            var queue = new Queue<T>();
            queue.Enqueue(root);
            while (queue.Count > 0)
            {
                var item = queue.Dequeue();
                yield return item;
                var children = childrenProvider(item);
                if (children != null)
                    foreach (var c in children)
                        queue.Enqueue(c);
            }
        }

        /// <summary>
        ///     LRN
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        private static IEnumerable<T> TraversePostOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            return TraverseReverseInOrder(root, childrenProvider).Reverse();
        }

        /// <summary>
        ///     NLR
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        private static IEnumerable<T> TraversePreOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            if (childrenProvider == null)
                throw new ArgumentNullException(nameof(childrenProvider));

            return TraverseReverseInOrder(root, x => childrenProvider(x)?.Reverse());
        }

        /// <summary>
        ///     RNL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        private static IEnumerable<T> TraverseReverseInOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            if (childrenProvider == null)
                throw new ArgumentNullException(nameof(childrenProvider));

            var stack = new Stack<T>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var item = stack.Pop();
                yield return item;
                var children = childrenProvider(item);
                if (children != null)
                    foreach (var c in children)
                        stack.Push(c);
            }
        }
    }
}