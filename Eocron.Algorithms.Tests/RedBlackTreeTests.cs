using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Tree;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class RedBlackTreeTests
    {
        private List<KeyValuePair<int, string>> GetTestItems()
        {
            return Enumerable
                .Range(0, 1000)
                .Select(x => new KeyValuePair<int, string>(x, Guid.NewGuid().ToString()))
                .ToList();
        }

        [Test]
        public void Add()
        {
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>();
            foreach (var keyValuePair in items) dict.Add(keyValuePair);
            foreach (var keyValuePair in items) AssertExist(dict, keyValuePair);
            ClassicAssert.AreEqual(items.Count, dict.Count);
            ClassicAssert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            ClassicAssert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        [Test]
        public void Clear()
        {
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>();
            foreach (var keyValuePair in items) dict.Add(keyValuePair);
            dict.Clear();

            ClassicAssert.IsEmpty(dict);
            ClassicAssert.AreEqual(0, dict.Count);
            ClassicAssert.Throws<InvalidOperationException>(() => dict.GetMaxKeyValuePair());
            ClassicAssert.Throws<InvalidOperationException>(() => dict.GetMinKeyValuePair());
        }

        [Test]
        public void Set()
        {
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>();
            foreach (var keyValuePair in items) dict[keyValuePair.Key] = keyValuePair.Value;
            ClassicAssert.AreEqual(items.Count, dict.Count);
            ClassicAssert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            ClassicAssert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        [Test]
        public void Remove()
        {
            var rnd = new Random();
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>(items);

            var toDelete = Enumerable.Range(0, 200).Select(x => items[rnd.Next(items.Count)]).ToList();
            items.RemoveAll(x => toDelete.Contains(x));
            foreach (var keyValuePair in toDelete) dict.Remove(keyValuePair.Key);

            foreach (var keyValuePair in items) AssertExist(dict, keyValuePair);

            foreach (var keyValuePair in toDelete) AssertNotExist(dict, keyValuePair);
            ClassicAssert.AreEqual(items.Count, dict.Count);
            ClassicAssert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            ClassicAssert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        private void AssertNotExist<TKey, TValue>(RedBlackTree<TKey, TValue> dict, KeyValuePair<TKey, TValue> item)
        {
            ClassicAssert.IsFalse(dict.ContainsKey(item.Key));
            ClassicAssert.Throws<KeyNotFoundException>(() =>
            {
                var t = dict[item.Key];
            });
            TValue tmp;
            ClassicAssert.IsFalse(dict.TryGetValue(item.Key, out tmp));
            ClassicAssert.AreEqual(default(TValue), tmp);
            ClassicAssert.IsFalse(dict.Contains(item));
            ClassicAssert.IsFalse(dict.Remove(item.Key));
        }

        private void AssertExist<TKey, TValue>(IDictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> item)
        {
            ClassicAssert.IsTrue(dict.ContainsKey(item.Key));
            ClassicAssert.AreEqual(item.Value, dict[item.Key]);
            TValue tmp;
            ClassicAssert.IsTrue(dict.TryGetValue(item.Key, out tmp));
            ClassicAssert.AreEqual(item.Value, tmp);
            ClassicAssert.IsTrue(dict.Contains(item));
            ClassicAssert.Throws<ArgumentException>(() => { dict.Add(item); });
        }
    }
}