using System.Collections.Generic;

namespace Eocron.Algorithms.Tree
{
    public interface IRedBlackTree<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
        IEnumerable<KeyValuePair<TKey, TValue>> GetAllKeyValueReversed();
        KeyValuePair<TKey, TValue> GetMaxKeyValuePair();
        KeyValuePair<TKey, TValue> GetMinKeyValuePair();
    }
}