namespace Eocron.Algorithms.Levenstain
{
    public interface ILevenstainOptions<in TSource, in TTarget>
    {
        /// <summary>
        /// Calculates cost of create operation
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        float GetCreateCost(TTarget target);
        /// <summary>
        /// Calculates cost of delete operation
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        float GetDeleteCost(TSource source);
        /// <summary>
        /// Calculates cost of update operation
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        float GetUpdateCost(TSource source, TTarget target);
    }
}