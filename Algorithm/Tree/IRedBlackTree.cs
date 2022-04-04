using System.Collections.Generic;

namespace Eocron.Algorithms.Tree
{
    public interface IRedBlackTree<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        KeyValuePair<TKey, TValue> MinKeyValue();

        KeyValuePair<TKey, TValue> MaxKeyValue();
    }
}