using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Tree
{
    public sealed class RedBlackTree<TKey, TValue> : IRedBlackTree<TKey, TValue>
    {
        public RedBlackTree(IComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public RedBlackTree(IEnumerable<KeyValuePair<TKey, TValue>> items, IComparer<TKey> comparer = null) :
            this(comparer)
        {
            foreach (var keyValuePair in items) Add(keyValuePair);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddOrError(item.Key, item.Value);
        }

        public void Add(TKey key, TValue value)
        {
            AddOrError(key, value);
        }


        public void Clear()
        {
            _root = Null;
            Count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public bool ContainsKey(TKey key)
        {
            return FindNodeByKey(key) != null;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length - arrayIndex < Count)
                throw new ArgumentOutOfRangeException(nameof(array));
            var currentPosition = arrayIndex;
            foreach (var item in GetAll())
            {
                array.SetValue(item, currentPosition);
                currentPosition++;
            }
        }

        public IEnumerable<KeyValuePair<TKey, TValue>> GetAllKeyValueReversed()
        {
            return GetAllReversed();
        }

        public KeyValuePair<TKey, TValue> GetMaxKeyValuePair()
        {
            var workNode = _root;

            if (workNode == null || workNode == Null)
                throw TreeIsEmpty();

            while (workNode.Right != Null)
                workNode = workNode.Right;

            return new KeyValuePair<TKey, TValue>(workNode.Key, workNode.Value);
        }


        public KeyValuePair<TKey, TValue> GetMinKeyValuePair()
        {
            var workNode = _root;

            if (workNode == null || workNode == Null)
                throw TreeIsEmpty();

            while (workNode.Left != Null)
                workNode = workNode.Left;

            return new KeyValuePair<TKey, TValue>(workNode.Key, workNode.Value);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }


        public bool Remove(TKey key)
        {
            var node = FindNodeByKey(key);
            if (node == null)
                return false;
            Remove(node);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            var node = FindNodeByKey(key);
            if (node == null)
                return false;
            value = node.Value;
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                throw KeyNotFound();
            }
            set => AddOrUpdate(key, value);
        }

        public bool IsReadOnly => false;
        public ICollection<TKey> Keys => GetAll().Select(i => i.Key).ToList();
        public ICollection<TValue> Values => GetAll().Select(i => i.Value).ToList();

        public int Count { get; private set; }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => FindNodeByKey(key).Value;
            set => FindNodeByKey(key).Value = value;
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetAll().Select(i => i.Key);
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetAll().Select(i => i.Value);

        #region Private

        private Exception KeyAlreadyExist()
        {
            return new ArgumentException("Key already exist.");
        }

        private Exception KeyNotFound()
        {
            return new KeyNotFoundException();
        }

        private Exception TreeIsEmpty()
        {
            return new InvalidOperationException("Tree is empty");
        }

        private void AddOrUpdate(TKey key, TValue data)
        {
            var newNode = new RedBlackNode(key, data);
            var workNode = _root;

            while (workNode != Null)
            {
                newNode.Parent = workNode;
                var result = _comparer.Compare(key, workNode.Key);
                if (result == 0)
                {
                    workNode.Value = data;
                    return;
                }

                workNode = result > 0
                    ? workNode.Right
                    : workNode.Left;
            }

            if (newNode.Parent != null)
            {
                if (_comparer.Compare(newNode.Key, newNode.Parent.Key) > 0)
                    newNode.Parent.Right = newNode;
                else
                    newNode.Parent.Left = newNode;
            }
            else
            {
                _root = newNode;
            }

            FixAdd(newNode);
            Count++;
        }

        private void AddOrError(TKey key, TValue data)
        {
            var newNode = new RedBlackNode(key, data);
            var workNode = _root;

            while (workNode != Null)
            {
                newNode.Parent = workNode;
                var result = _comparer.Compare(key, workNode.Key);
                if (result == 0)
                    throw KeyAlreadyExist();
                workNode = result > 0
                    ? workNode.Right
                    : workNode.Left;
            }

            if (newNode.Parent != null)
            {
                if (_comparer.Compare(newNode.Key, newNode.Parent.Key) > 0)
                    newNode.Parent.Right = newNode;
                else
                    newNode.Parent.Left = newNode;
            }
            else
            {
                _root = newNode;
            }

            FixAdd(newNode);
            Count++;
        }

        private void Remove(RedBlackNode node)
        {
            RedBlackNode workNode;

            if (node.Left == Null || node.Right == Null)
            {
                workNode = node;
            }
            else
            {
                workNode = node.Right;
                while (workNode.Left != Null)
                    workNode = workNode.Left;
            }


            var linkedNode = workNode.Left != Null
                ? workNode.Left
                : workNode.Right;

            linkedNode.Parent = workNode.Parent;
            if (workNode.Parent != null)
                if (workNode == workNode.Parent.Left)
                    workNode.Parent.Left = linkedNode;
                else
                    workNode.Parent.Right = linkedNode;
            else
                _root = linkedNode;

            if (workNode != node)
            {
                node.Key = workNode.Key;
                node.Value = workNode.Value;
            }

            if (!workNode.Red)
                FixRemove(linkedNode);

            Count--;
        }

        private void FixRemove(RedBlackNode node)
        {
            while (node != _root && !node.Red)
            {
                RedBlackNode workNode;
                if (node == node.Parent.Left)
                {
                    workNode = node.Parent.Right;
                    if (workNode.Red)
                    {
                        node.Parent.Red = true;
                        workNode.Red = false;
                        RotateLeft(node.Parent);
                        workNode = node.Parent.Right;
                    }

                    if (!workNode.Left.Red &&
                        !workNode.Right.Red)
                    {
                        workNode.Red = true;
                        node = node.Parent;
                    }
                    else
                    {
                        if (!workNode.Right.Red)
                        {
                            workNode.Left.Red = false;
                            workNode.Red = true;
                            RotateRight(workNode);
                            workNode = node.Parent.Right;
                        }

                        node.Parent.Red = false;
                        workNode.Red = node.Parent.Red;
                        workNode.Right.Red = false;
                        RotateLeft(node.Parent);
                        node = _root;
                    }
                }
                else
                {
                    workNode = node.Parent.Left;
                    if (workNode.Red)
                    {
                        node.Parent.Red = true;
                        workNode.Red = false;
                        RotateRight(node.Parent);
                        workNode = node.Parent.Left;
                    }

                    if (!workNode.Right.Red &&
                        !workNode.Left.Red)
                    {
                        workNode.Red = true;
                        node = node.Parent;
                    }
                    else
                    {
                        if (!workNode.Left.Red)
                        {
                            workNode.Right.Red = false;
                            workNode.Red = true;
                            RotateLeft(workNode);
                            workNode = node.Parent.Left;
                        }

                        workNode.Red = node.Parent.Red;
                        node.Parent.Red = false;
                        workNode.Left.Red = false;
                        RotateRight(node.Parent);
                        node = _root;
                    }
                }
            }

            node.Red = false;
        }

        private Stack<KeyValuePair<TKey, TValue>> GetAll()
        {
            var stack = new Stack<KeyValuePair<TKey, TValue>>(Count);
            if (_root != Null) GetAllRecursive(_root, stack);
            return stack;
        }

        private static void GetAllRecursive(RedBlackNode node, Stack<KeyValuePair<TKey, TValue>> stack)
        {
            if (node.Right != Null)
                GetAllRecursive(node.Right, stack);
            stack.Push(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            if (node.Left != Null)
                GetAllRecursive(node.Left, stack);
        }

        private Stack<KeyValuePair<TKey, TValue>> GetAllReversed()
        {
            var stack = new Stack<KeyValuePair<TKey, TValue>>(Count);
            if (_root != Null) GetAllReversedRecursive(_root, stack);
            return stack;
        }

        private static void GetAllReversedRecursive(RedBlackNode node, Stack<KeyValuePair<TKey, TValue>> stack)
        {
            if (node.Left != Null)
                GetAllReversedRecursive(node.Left, stack);
            stack.Push(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            if (node.Right != Null)
                GetAllReversedRecursive(node.Right, stack);
        }

        private RedBlackNode FindNodeByKey(TKey key)
        {
            var treeNode = _root;

            while (treeNode != Null)
            {
                var result = _comparer.Compare(key, treeNode.Key);
                if (result == 0) return treeNode;
                treeNode = result < 0
                    ? treeNode.Left
                    : treeNode.Right;
            }

            return null;
        }

        private void RotateRight(RedBlackNode rotateNode)
        {
            var workNode = rotateNode.Left;

            rotateNode.Left = workNode.Right;

            if (workNode.Right != Null)
                workNode.Right.Parent = rotateNode;

            if (workNode != Null)
                workNode.Parent = rotateNode.Parent;

            if (rotateNode.Parent != null)
            {
                if (rotateNode == rotateNode.Parent.Right)
                    rotateNode.Parent.Right = workNode;
                else
                    rotateNode.Parent.Left = workNode;
            }
            else
            {
                _root = workNode;
            }

            workNode.Right = rotateNode;
            if (rotateNode != Null)
                rotateNode.Parent = workNode;
        }

        private void RotateLeft(RedBlackNode node)
        {
            var workNode = node.Right;

            node.Right = workNode.Left;

            if (workNode.Left != Null)
                workNode.Left.Parent = node;

            if (workNode != Null)
                workNode.Parent = node.Parent;

            if (node.Parent != null)
            {
                if (node == node.Parent.Left)
                    node.Parent.Left = workNode;
                else
                    node.Parent.Right = workNode;
            }
            else
            {
                _root = workNode;
            }

            workNode.Left = node;
            if (node != Null)
                node.Parent = workNode;
        }

        private void FixAdd(RedBlackNode node)
        {
            while (node != _root && node.Parent.Red)
            {
                RedBlackNode workNode;
                if (node.Parent == node.Parent.Parent.Left)
                {
                    workNode = node.Parent.Parent.Right;
                    if (workNode != null && workNode.Red)
                    {
                        node.Parent.Red = false;
                        workNode.Red = false;
                        node.Parent.Parent.Red = true;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (node == node.Parent.Right)
                        {
                            node = node.Parent;
                            RotateLeft(node);
                        }

                        node.Parent.Red = false;
                        node.Parent.Parent.Red = true;
                        RotateRight(node.Parent.Parent);
                    }
                }
                else
                {
                    workNode = node.Parent.Parent.Left;
                    if (workNode != null && workNode.Red)
                    {
                        node.Parent.Red = false;
                        workNode.Red = false;
                        node.Parent.Parent.Red = true;
                        node = node.Parent.Parent;
                    }
                    else
                    {
                        if (node == node.Parent.Left)
                        {
                            node = node.Parent;
                            RotateRight(node);
                        }

                        node.Parent.Red = false;
                        node.Parent.Parent.Red = true;
                        RotateLeft(node.Parent.Parent);
                    }
                }
            }

            _root.Red = false;
        }

        private readonly IComparer<TKey> _comparer;
        private RedBlackNode _root = Null;

        private static readonly RedBlackNode Null =
            new RedBlackNode
            {
                Left = null,
                Right = null,
                Parent = null,
                Red = false
            };

        private sealed class RedBlackNode
        {
            public RedBlackNode()
            {
                Red = true;
                Right = Null;
                Left = Null;
            }

            public RedBlackNode(TKey key, TValue data)
                : this()
            {
                Key = key;
                Value = data;
            }

            public bool Red;
            public RedBlackNode Left;
            public RedBlackNode Parent;
            public RedBlackNode Right;
            public TKey Key;
            public TValue Value;
        }

        #endregion
    }
}