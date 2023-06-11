using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Levenstain
{
    public sealed class LevenstainAlgorithm<TSource, TTarget> : ILevenstainAlgorithm<TSource, TTarget>
    {
        private LevenstainAlgorithm()
        {
            Options = new DefaultLevenstainOptions<TSource, TTarget>();
        }

        /// <summary>
        ///     Calculates set of remarks which need to do to transform source to target.
        ///     Asymptotic worst case: O(max(n,m))
        ///     Memory asymptotic worst case: O(1)
        /// </summary>
        /// <typeparam name="TEdit"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="editSelector">Remark provider.</param>
        /// <param name="reverse">Reverse result.</param>
        /// <returns></returns>
        public IEnumerable<TEdit> CalculateEdit<TEdit>(
            IList<TSource> source,
            IList<TTarget> target,
            Func<TSource, TTarget, TEdit> editSelector,
            bool reverse = false)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (editSelector == null)
                throw new ArgumentNullException(nameof(editSelector));
            if (Options == null)
                throw new ArgumentNullException(nameof(Options));
            return CalculateEdit(CalculateMatrix(source, target), source, target, editSelector, reverse);
        }

        public IEnumerable<TEdit> CalculateEdit<TEdit>(
            ILevenstainMatrix matrix,
            IList<TSource> source,
            IList<TTarget> target,
            Func<TSource, TTarget, TEdit> editSelector,
            bool reverse = false)
        {
            var result = InternalCalculateEdit(matrix, source, target, editSelector);
            return reverse ? result : result.Reverse(); //default is already reversed
        }

        /// <summary>
        ///     Calculates dynamic Levenstain matrix of weights.
        ///     Asymptotic worst case: O(n*m)
        ///     Memory asymptotic worst case: O(n*m)
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public ILevenstainMatrix CalculateMatrix(IList<TSource> source, IList<TTarget> target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (Options == null)
                throw new ArgumentNullException(nameof(Options));
            var m = source.Count + 1;
            var n = target.Count + 1;
            var matrix = new LevenstainMatrix(m, n);
            for (var i = 1; i < m; i++)
                matrix[i, 0] = matrix[i - 1, 0] + Options.GetDeleteCost(source[i - 1]);
            for (var j = 1; j < n; j++)
                matrix[0, j] = matrix[0, j - 1] + Options.GetCreateCost(target[j - 1]);


            for (var i = 1; i < m; i++)
            for (var j = 1; j < n; j++)
            {
                var ss1 = source[i - 1];
                var ss2 = target[j - 1];
                var diag = matrix[i - 1, j - 1] + Options.GetUpdateCost(ss1, ss2);
                var left = matrix[i, j - 1] + Options.GetCreateCost(ss2);
                var up = matrix[i - 1, j] + Options.GetDeleteCost(ss1);
                if (diag <= left && diag <= up)
                    matrix[i, j] = diag;
                else if (left <= diag && left <= up)
                    matrix[i, j] = left;
                else if (up <= diag && up <= left)
                    matrix[i, j] = up;
                else
                    throw new InvalidOperationException();
            }

            return matrix;
        }

        public static ILevenstainAlgorithm<TSource, TTarget> Create()
        {
            return new LevenstainAlgorithm<TSource, TTarget>();
        }

        private IEnumerable<TEdit> InternalCalculateEdit<TEdit>(
            ILevenstainMatrix matrix,
            IList<TSource> source,
            IList<TTarget> target,
            Func<TSource, TTarget, TEdit> editSelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (editSelector == null)
                throw new ArgumentNullException(nameof(editSelector));
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.M != source.Count + 1 || matrix.N != target.Count + 1)
                throw new ArgumentOutOfRangeException("Invalid matrix size.");
            if (Options == null)
                throw new ArgumentNullException(nameof(Options));
            var i = matrix.M - 1;
            var j = matrix.N - 1;
            while (i != 0 || j != 0)
            {
                if (i == 0)
                {
                    yield return editSelector(default, target[j - 1]);
                    j--;
                    continue;
                }

                if (j == 0)
                {
                    yield return editSelector(source[i - 1], default);
                    i--;
                    continue;
                }

                var ss1 = source[i - 1];
                var ss2 = target[j - 1];
                var diag = matrix[i - 1, j - 1] + Options.GetUpdateCost(ss1, ss2);
                var left = matrix[i, j - 1] + Options.GetCreateCost(ss2);
                var up = matrix[i - 1, j] + Options.GetDeleteCost(ss1);
                if (diag <= left && diag <= up)
                {
                    yield return editSelector(ss1, ss2);
                    i--;
                    j--;
                }
                else if (left <= diag && left <= up)
                {
                    yield return editSelector(default, ss2);
                    j--;
                }
                else if (up <= diag && up <= left)
                {
                    yield return editSelector(ss1, default);
                    i--;
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public ILevenstainOptions<TSource, TTarget> Options { get; set; }
    }
}