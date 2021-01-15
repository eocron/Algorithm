using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithm.Tree
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Tree_traversal
    /// </summary>
    public static class TreeTraversalExtensions
    {
        /// <summary>
        /// RNL
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        public static IEnumerable<T> TraverseReverseInOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            var stack = new Stack<T>();
            stack.Push(root);
            while(stack.Count > 0)
            {
                var item = stack.Pop();
                yield return item;
                var children = childrenProvider(item);
                if(children != null)
                {
                    foreach (var c in children)
                        stack.Push(c);
                }
            }
        }

        /// <summary>
        /// NLR
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        public static IEnumerable<T> TraversePreOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            return TraverseReverseInOrder(root, (x) => childrenProvider(x)?.Reverse());
        }

        /// <summary>
        /// LRN
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        public static IEnumerable<T> TraversePostOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            return TraverseReverseInOrder(root, childrenProvider).Reverse();
        }

        /// <summary>
        /// BFS
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="root"></param>
        /// <param name="childrenProvider"></param>
        /// <returns></returns>
        public static IEnumerable<T> TraverseLevelOrder<T>(this T root, Func<T, IEnumerable<T>> childrenProvider)
        {
            var queue = new Queue<T>();
            queue.Enqueue(root);
            while(queue.Count > 0)
            {
                var item = queue.Dequeue();
                    yield return item;
                var children = childrenProvider(item);
                if (children != null)
                {
                    foreach (var c in children)
                    {
                        queue.Enqueue(c);
                    }
                }
            }
        }
    }
}
