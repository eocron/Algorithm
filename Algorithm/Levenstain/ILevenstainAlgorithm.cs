using System;
using System.Collections.Generic;

namespace Algorithm.Levenstain
{
    public interface ILevenstainAlgorithm<TSource, TTarget>
    {
        /// <summary>
        /// Options which will be used upon calculation (weight parameters, equality, etc).
        /// </summary>
        ILevenstainOptions<TSource, TTarget> Options { get; set; }

        /// <summary>
        /// Calculate action required to transform source sequence to target sequence.
        /// </summary>
        /// <typeparam name="TEdit"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="editSelector"></param>
        /// <param name="matrix"></param>
        /// <param name="reverse"></param>
        /// <returns></returns>
        IEnumerable<TEdit> CalculateEdit<TEdit>(
            ILevenstainMatrix matrix, 
            IList<TSource> source, 
            IList<TTarget> target, 
            Func<TSource, TTarget, TEdit> editSelector,
            bool reverse = false);

        /// <summary>
        /// Calculate action required to transform source sequence to target sequence.
        /// </summary>
        /// <typeparam name="TEdit"></typeparam>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <param name="editSelector"></param>
        /// <param name="reverse"></param>
        /// <returns></returns>
        IEnumerable<TEdit> CalculateEdit<TEdit>(
            IList<TSource> source, 
            IList<TTarget> target, 
            Func<TSource, TTarget, TEdit> editSelector,
            bool reverse = false);

        /// <summary>
        /// Calculates Levenstain weight matrix.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        ILevenstainMatrix CalculateMatrix(IList<TSource> source, IList<TTarget> target);
    }
}