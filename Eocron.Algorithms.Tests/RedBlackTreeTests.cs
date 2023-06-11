using System;
using System.Collections.Generic;
using System.Linq;
using Eocron.Algorithms.Tree;
using NUnit.Framework;

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
            foreach (var keyValuePair in items)
            {
                dict.Add(keyValuePair);
            }
            foreach (var keyValuePair in items)
            {
                AssertExist(dict, keyValuePair);
            }
            Assert.AreEqual(items.Count, dict.Count);
            Assert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            Assert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        [Test]
        public void Clear()
        {
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>();
            foreach (var keyValuePair in items)
            {
                dict.Add(keyValuePair);
            }
            dict.Clear();

            Assert.IsEmpty(dict);
            Assert.AreEqual(0, dict.Count);
            Assert.Throws<InvalidOperationException>(() => dict.GetMaxKeyValuePair());
            Assert.Throws<InvalidOperationException>(() => dict.GetMinKeyValuePair());
        }

        [Test]
        public void Set()
        {
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>();
            foreach (var keyValuePair in items)
            {
                dict[keyValuePair.Key] = keyValuePair.Value;
            }
            Assert.AreEqual(items.Count, dict.Count);
            Assert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            Assert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        [Test]
        public void Remove()
        {
            var rnd = new Random();
            var items = GetTestItems();
            var dict = new RedBlackTree<int, string>(items);

            var toDelete = Enumerable.Range(0, 200).Select(x=> items[rnd.Next(items.Count)]).ToList();
            items.RemoveAll(x => toDelete.Contains(x));
            foreach (var keyValuePair in toDelete)
            {
                dict.Remove(keyValuePair.Key);
            }

            foreach (var keyValuePair in items)
            {
                AssertExist(dict, keyValuePair);
            }

            foreach (var keyValuePair in toDelete)
            {
                AssertNotExist(dict, keyValuePair);
            }
            Assert.AreEqual(items.Count, dict.Count);
            Assert.AreEqual(items.First(), dict.GetMinKeyValuePair());
            Assert.AreEqual(items.Last(), dict.GetMaxKeyValuePair());
            CollectionAssert.AreEquivalent(items, dict);
        }

        private void AssertNotExist<TKey, TValue>(RedBlackTree<TKey, TValue> dict, KeyValuePair<TKey, TValue> item)
        {
            Assert.IsFalse(dict.ContainsKey(item.Key));
            Assert.Throws<KeyNotFoundException>(()=>
            {
                var t = dict[item.Key];
            });
            TValue tmp;
            Assert.IsFalse(dict.TryGetValue(item.Key, out tmp));
            Assert.AreEqual(default(TValue), tmp);
            Assert.IsFalse(dict.Contains(item));
            Assert.IsFalse(dict.Remove(item.Key));
        }

        private void AssertExist<TKey, TValue>(IDictionary<TKey, TValue> dict, KeyValuePair<TKey, TValue> item)
        {
            Assert.IsTrue(dict.ContainsKey(item.Key));
            Assert.AreEqual(item.Value, dict[item.Key]);
            TValue tmp;
            Assert.IsTrue(dict.TryGetValue(item.Key, out tmp));
            Assert.AreEqual(item.Value, tmp);
            Assert.IsTrue(dict.Contains(item));
            Assert.Throws<ArgumentException>(() =>
            {
                dict.Add(item);
            });
        }
    }
}
