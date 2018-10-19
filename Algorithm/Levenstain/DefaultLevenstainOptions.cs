using System.Collections;
using System.Collections.Generic;

namespace Algorithm.Levenstain
{
    public class DefaultLevenstainOptions<TSource, TTarget> : ILevenstainOptions<TSource, TTarget>
    {
        private readonly IEqualityComparer _comparer;

        public DefaultLevenstainOptions(IEqualityComparer comparer = null)
        {
            _comparer = comparer ?? EqualityComparer<object>.Default;
        }

        public virtual float GetCreateCost(TTarget target)
        {
            return 1;
        }

        public virtual float GetDeleteCost(TSource source)
        {
            return 1;
        }

        public virtual float GetUpdateCost(TSource source, TTarget target)
        {
            var res = _comparer.Equals(source,target);
            return res ? 0f : 1f;
        }
    }
}