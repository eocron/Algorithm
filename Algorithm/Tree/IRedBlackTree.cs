using System.Collections.Generic;

namespace Eocron.Algorithms.Tree
{
    public interface IRedBlackTree<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        KeyValuePair<TKey, TValue> GetMinKeyValuePair();
        KeyValuePair<TKey, TValue> GetMaxKeyValuePair();
        IEnumerable<KeyValuePair<TKey, TValue>> GetAllKeyValueReversed();
    }
}