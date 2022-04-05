using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                throw KeyNotFound();
            }
            set => GetNode(key).Data = value;
        }


        public int Count => _count;
        public bool IsReadOnly => false;


        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }


        public KeyValuePair<TKey, TValue> GetMinKeyValuePair()
        {
            var workNode = _treeBaseNode;

            if (workNode == null || workNode == SentinelNode)
                throw TreeIsEmpty();

            while (workNode.Left != SentinelNode)
                workNode = workNode.Left;

            return new KeyValuePair<TKey, TValue>(workNode.Key, workNode.Data);
        }

        public KeyValuePair<TKey, TValue> GetMaxKeyValuePair()
        {
            var workNode = _treeBaseNode;

            if (workNode == null || workNode == SentinelNode)
                throw TreeIsEmpty();

            while (workNode.Right != SentinelNode)
                workNode = workNode.Right;

            return new KeyValuePair<TKey, TValue>(workNode.Key, workNode.Data);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            New(item.Key, item.Value);
        }


        public void Clear()
        {
            _treeBaseNode = SentinelNode;
            _count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsKey(item.Key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length - arrayIndex < _count)
                throw new ArgumentOutOfRangeException(nameof(array));
            var currentPosition = arrayIndex;
            foreach (var item in GetAll())
            {
                array.SetValue(item, currentPosition);
                currentPosition++;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetAll().GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            var node = GetNode(key);
            return node != null;
        }

        public void Add(TKey key, TValue value)
        {
            New(key, value);
        }


        public bool Remove(TKey key)
        {
            try
            {
                Delete(GetNode(key));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            value = default;
            var node = GetNode(key);
            if (node == null)
                return false;
            value = node.Data;
            return true;
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => GetNode(key).Data;
            set => GetNode(key).Data = value;
        }


        public ICollection<TKey> Keys
        {
            get { return GetAll().Select(i => i.Key).ToList(); }
        }

        public ICollection<TValue> Values
        {
            get { return GetAll().Select(i => i.Value).ToList(); }
        }

        

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

        private void New(TKey key, TValue data)
        {
            var newNode = new RedBlackNode(key, data);
            var workNode = _treeBaseNode;

            while (workNode != SentinelNode)
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
                _treeBaseNode = newNode;
            }

            BalanceTreeAfterInsert(newNode);
            Interlocked.Increment(ref _count);
        }

        private void Delete(RedBlackNode deleteNode)
        {
            RedBlackNode workNode;

            if (deleteNode.Left == SentinelNode || deleteNode.Right == SentinelNode)
            {
                workNode = deleteNode;
            }
            else
            {
                workNode = deleteNode.Right;
                while (workNode.Left != SentinelNode)
                    workNode = workNode.Left;
            }


            var linkedNode = workNode.Left != SentinelNode
                ? workNode.Left
                : workNode.Right;

            linkedNode.Parent = workNode.Parent;
            if (workNode.Parent != null)
                if (workNode == workNode.Parent.Left)
                    workNode.Parent.Left = linkedNode;
                else
                    workNode.Parent.Right = linkedNode;
            else
                _treeBaseNode = linkedNode;

            if (workNode != deleteNode)
            {
                deleteNode.Key = workNode.Key;
                deleteNode.Data = workNode.Data;
            }

            if (workNode.Color == RedBlackNodeType.Black)
                BalanceTreeAfterDelete(linkedNode);

            Interlocked.Decrement(ref _count);
        }

        private void BalanceTreeAfterDelete(RedBlackNode linkedNode)
        {
            while (linkedNode != _treeBaseNode && linkedNode.Color == RedBlackNodeType.Black)
            {
                RedBlackNode workNode;
                if (linkedNode == linkedNode.Parent.Left)
                {
                    workNode = linkedNode.Parent.Right;
                    if (workNode.Color == RedBlackNodeType.Red)
                    {
                        linkedNode.Parent.Color = RedBlackNodeType.Red;
                        workNode.Color = RedBlackNodeType.Black;
                        RotateLeft(linkedNode.Parent);
                        workNode = linkedNode.Parent.Right;
                    }

                    if (workNode.Left.Color == RedBlackNodeType.Black &&
                        workNode.Right.Color == RedBlackNodeType.Black)
                    {
                        workNode.Color = RedBlackNodeType.Red;
                        linkedNode = linkedNode.Parent;
                    }
                    else
                    {
                        if (workNode.Right.Color == RedBlackNodeType.Black)
                        {
                            workNode.Left.Color = RedBlackNodeType.Black;
                            workNode.Color = RedBlackNodeType.Red;
                            RotateRight(workNode);
                            workNode = linkedNode.Parent.Right;
                        }

                        linkedNode.Parent.Color = RedBlackNodeType.Black;
                        workNode.Color = linkedNode.Parent.Color;
                        workNode.Right.Color = RedBlackNodeType.Black;
                        RotateLeft(linkedNode.Parent);
                        linkedNode = _treeBaseNode;
                    }
                }
                else
                {
                    workNode = linkedNode.Parent.Left;
                    if (workNode.Color == RedBlackNodeType.Red)
                    {
                        linkedNode.Parent.Color = RedBlackNodeType.Red;
                        workNode.Color = RedBlackNodeType.Black;
                        RotateRight(linkedNode.Parent);
                        workNode = linkedNode.Parent.Left;
                    }

                    if (workNode.Right.Color == RedBlackNodeType.Black &&
                        workNode.Left.Color == RedBlackNodeType.Black)
                    {
                        workNode.Color = RedBlackNodeType.Red;
                        linkedNode = linkedNode.Parent;
                    }
                    else
                    {
                        if (workNode.Left.Color == RedBlackNodeType.Black)
                        {
                            workNode.Right.Color = RedBlackNodeType.Black;
                            workNode.Color = RedBlackNodeType.Red;
                            RotateLeft(workNode);
                            workNode = linkedNode.Parent.Left;
                        }

                        workNode.Color = linkedNode.Parent.Color;
                        linkedNode.Parent.Color = RedBlackNodeType.Black;
                        workNode.Left.Color = RedBlackNodeType.Black;
                        RotateRight(linkedNode.Parent);
                        linkedNode = _treeBaseNode;
                    }
                }
            }

            linkedNode.Color = RedBlackNodeType.Black;
        }

        private Stack<KeyValuePair<TKey, TValue>> GetAll()
        {
            var stack = new Stack<KeyValuePair<TKey, TValue>>(_count);

            if (_treeBaseNode != SentinelNode) WalkNextLevel(_treeBaseNode, stack);
            return stack;
        }

        private static void WalkNextLevel(RedBlackNode node, Stack<KeyValuePair<TKey, TValue>> stack)
        {
            if (node.Right != SentinelNode)
                WalkNextLevel(node.Right, stack);
            stack.Push(new KeyValuePair<TKey, TValue>(node.Key, node.Data));
            if (node.Left != SentinelNode)
                WalkNextLevel(node.Left, stack);
        }

        private RedBlackNode GetNode(TKey key)
        {
            var treeNode = _treeBaseNode;

            while (treeNode != SentinelNode)
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

            if (workNode.Right != SentinelNode)
                workNode.Right.Parent = rotateNode;

            if (workNode != SentinelNode)
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
                _treeBaseNode = workNode;
            }

            workNode.Right = rotateNode;
            if (rotateNode != SentinelNode)
                rotateNode.Parent = workNode;
        }

        private void RotateLeft(RedBlackNode rotateNode)
        {
            var workNode = rotateNode.Right;

            rotateNode.Right = workNode.Left;

            if (workNode.Left != SentinelNode)
                workNode.Left.Parent = rotateNode;

            if (workNode != SentinelNode)
                workNode.Parent = rotateNode.Parent;

            if (rotateNode.Parent != null)
            {
                if (rotateNode == rotateNode.Parent.Left)
                    rotateNode.Parent.Left = workNode;
                else
                    rotateNode.Parent.Right = workNode;
            }
            else
            {
                _treeBaseNode = workNode;
            }

            workNode.Left = rotateNode;
            if (rotateNode != SentinelNode)
                rotateNode.Parent = workNode;
        }

        private void BalanceTreeAfterInsert(RedBlackNode insertedNode)
        {
            while (insertedNode != _treeBaseNode && insertedNode.Parent.Color == RedBlackNodeType.Red)
            {
                RedBlackNode workNode;
                if (insertedNode.Parent == insertedNode.Parent.Parent.Left)
                {
                    workNode = insertedNode.Parent.Parent.Right;
                    if (workNode != null && workNode.Color == RedBlackNodeType.Red)
                    {
                        insertedNode.Parent.Color = RedBlackNodeType.Black;
                        workNode.Color = RedBlackNodeType.Black;
                        insertedNode.Parent.Parent.Color = RedBlackNodeType.Red;
                        insertedNode = insertedNode.Parent.Parent;
                    }
                    else
                    {
                        if (insertedNode == insertedNode.Parent.Right)
                        {
                            insertedNode = insertedNode.Parent;
                            RotateLeft(insertedNode);
                        }

                        insertedNode.Parent.Color = RedBlackNodeType.Black;
                        insertedNode.Parent.Parent.Color = RedBlackNodeType.Red;
                        RotateRight(insertedNode.Parent.Parent);
                    }
                }
                else
                {
                    workNode = insertedNode.Parent.Parent.Left;
                    if (workNode != null && workNode.Color == RedBlackNodeType.Red)
                    {
                        insertedNode.Parent.Color = RedBlackNodeType.Black;
                        workNode.Color = RedBlackNodeType.Black;
                        insertedNode.Parent.Parent.Color = RedBlackNodeType.Red;
                        insertedNode = insertedNode.Parent.Parent;
                    }
                    else
                    {
                        if (insertedNode == insertedNode.Parent.Left)
                        {
                            insertedNode = insertedNode.Parent;
                            RotateRight(insertedNode);
                        }

                        insertedNode.Parent.Color = RedBlackNodeType.Black;
                        insertedNode.Parent.Parent.Color = RedBlackNodeType.Red;
                        RotateLeft(insertedNode.Parent.Parent);
                    }
                }
            }

            _treeBaseNode.Color = RedBlackNodeType.Black;
        }

        private static readonly RedBlackNode SentinelNode =
            new RedBlackNode
            {
                Left = null,
                Right = null,
                Parent = null,
                Color = RedBlackNodeType.Black
            };

        private readonly IComparer<TKey> _comparer;
        private int _count;
        private RedBlackNode _treeBaseNode = SentinelNode;

        private enum RedBlackNodeType
        {
            Red = 0,
            Black = 1
        }

        private sealed class RedBlackNode
        {
            public RedBlackNode()
            {
                Color = RedBlackNodeType.Red;

                Right = SentinelNode;
                Left = SentinelNode;
            }

            public RedBlackNode(TKey key, TValue data)
                : this()
            {
                Key = key;
                Data = data;
            }

            public TValue Data { get; set; }

            public TKey Key { get; set; }

            public RedBlackNodeType Color { get; set; }

            public RedBlackNode Left { get; set; }

            public RedBlackNode Right { get; set; }

            public RedBlackNode Parent { get; set; }
        }
        #endregion
    }
}