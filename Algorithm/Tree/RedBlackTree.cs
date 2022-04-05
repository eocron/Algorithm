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

        public int Count => _count;
        public bool IsReadOnly => false;


        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
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

        public KeyValuePair<TKey, TValue> GetMaxKeyValuePair()
        {
            var workNode = _root;

            if (workNode == null || workNode == Null)
                throw TreeIsEmpty();

            while (workNode.Right != Null)
                workNode = workNode.Right;

            return new KeyValuePair<TKey, TValue>(workNode.Key, workNode.Value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddOrError(item.Key, item.Value);
        }


        public void Clear()
        {
            _root = Null;
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
            var node = FindNodeByKey(key);
            return node != null;
        }

        public void Add(TKey key, TValue value)
        {
            AddOrError(key, value);
        }


        public bool Remove(TKey key)
        {
            try
            {
                Delete(FindNodeByKey(key));
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
            var node = FindNodeByKey(key);
            if (node == null)
                return false;
            value = node.Value;
            return true;
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => FindNodeByKey(key).Value;
            set => FindNodeByKey(key).Value = value;
        }


        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => GetAll().Select(i => i.Key);

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => GetAll().Select(i => i.Value);

        public ICollection<TKey> Keys => GetAll().Select(i => i.Key).ToList();
        public ICollection<TValue> Values => GetAll().Select(i => i.Value).ToList();

        

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

            BalanceTreeAfterInsert(newNode);
            _count++;
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

            BalanceTreeAfterInsert(newNode);
            _count++;
        }

        private void Delete(RedBlackNode deleteNode)
        {
            RedBlackNode workNode;

            if (deleteNode.Left == Null || deleteNode.Right == Null)
            {
                workNode = deleteNode;
            }
            else
            {
                workNode = deleteNode.Right;
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

            if (workNode != deleteNode)
            {
                deleteNode.Key = workNode.Key;
                deleteNode.Value = workNode.Value;
            }

            if (!workNode.Red)
                BalanceTreeAfterDelete(linkedNode);

            _count--;
        }

        private void BalanceTreeAfterDelete(RedBlackNode linkedNode)
        {
            while (linkedNode != _root && !linkedNode.Red)
            {
                RedBlackNode workNode;
                if (linkedNode == linkedNode.Parent.Left)
                {
                    workNode = linkedNode.Parent.Right;
                    if (workNode.Red)
                    {
                        linkedNode.Parent.Red = true;
                        workNode.Red = false;
                        RotateLeft(linkedNode.Parent);
                        workNode = linkedNode.Parent.Right;
                    }

                    if (!workNode.Left.Red &&
                        !workNode.Right.Red)
                    {
                        workNode.Red = true;
                        linkedNode = linkedNode.Parent;
                    }
                    else
                    {
                        if (!workNode.Right.Red)
                        {
                            workNode.Left.Red = false;
                            workNode.Red = true;
                            RotateRight(workNode);
                            workNode = linkedNode.Parent.Right;
                        }

                        linkedNode.Parent.Red = false;
                        workNode.Red = linkedNode.Parent.Red;
                        workNode.Right.Red = false;
                        RotateLeft(linkedNode.Parent);
                        linkedNode = _root;
                    }
                }
                else
                {
                    workNode = linkedNode.Parent.Left;
                    if (workNode.Red)
                    {
                        linkedNode.Parent.Red = true;
                        workNode.Red = false;
                        RotateRight(linkedNode.Parent);
                        workNode = linkedNode.Parent.Left;
                    }

                    if (!workNode.Right.Red &&
                        !workNode.Left.Red)
                    {
                        workNode.Red = true;
                        linkedNode = linkedNode.Parent;
                    }
                    else
                    {
                        if (!workNode.Left.Red)
                        {
                            workNode.Right.Red = false;
                            workNode.Red = true;
                            RotateLeft(workNode);
                            workNode = linkedNode.Parent.Left;
                        }

                        workNode.Red = linkedNode.Parent.Red;
                        linkedNode.Parent.Red = false;
                        workNode.Left.Red = false;
                        RotateRight(linkedNode.Parent);
                        linkedNode = _root;
                    }
                }
            }

            linkedNode.Red = false;
        }

        private Stack<KeyValuePair<TKey, TValue>> GetAll()
        {
            var stack = new Stack<KeyValuePair<TKey, TValue>>(_count);

            if (_root != Null) WalkNextLevel(_root, stack);
            return stack;
        }

        private static void WalkNextLevel(RedBlackNode node, Stack<KeyValuePair<TKey, TValue>> stack)
        {
            if (node.Right != Null)
                WalkNextLevel(node.Right, stack);
            stack.Push(new KeyValuePair<TKey, TValue>(node.Key, node.Value));
            if (node.Left != Null)
                WalkNextLevel(node.Left, stack);
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

        private void RotateLeft(RedBlackNode rotateNode)
        {
            var workNode = rotateNode.Right;

            rotateNode.Right = workNode.Left;

            if (workNode.Left != Null)
                workNode.Left.Parent = rotateNode;

            if (workNode != Null)
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
                _root = workNode;
            }

            workNode.Left = rotateNode;
            if (rotateNode != Null)
                rotateNode.Parent = workNode;
        }

        private void BalanceTreeAfterInsert(RedBlackNode insertedNode)
        {
            while (insertedNode != _root && insertedNode.Parent.Red)
            {
                RedBlackNode workNode;
                if (insertedNode.Parent == insertedNode.Parent.Parent.Left)
                {
                    workNode = insertedNode.Parent.Parent.Right;
                    if (workNode != null && workNode.Red)
                    {
                        insertedNode.Parent.Red = false;
                        workNode.Red = false;
                        insertedNode.Parent.Parent.Red = true;
                        insertedNode = insertedNode.Parent.Parent;
                    }
                    else
                    {
                        if (insertedNode == insertedNode.Parent.Right)
                        {
                            insertedNode = insertedNode.Parent;
                            RotateLeft(insertedNode);
                        }

                        insertedNode.Parent.Red = false;
                        insertedNode.Parent.Parent.Red = true;
                        RotateRight(insertedNode.Parent.Parent);
                    }
                }
                else
                {
                    workNode = insertedNode.Parent.Parent.Left;
                    if (workNode != null && workNode.Red)
                    {
                        insertedNode.Parent.Red = false;
                        workNode.Red = false;
                        insertedNode.Parent.Parent.Red = true;
                        insertedNode = insertedNode.Parent.Parent;
                    }
                    else
                    {
                        if (insertedNode == insertedNode.Parent.Left)
                        {
                            insertedNode = insertedNode.Parent;
                            RotateRight(insertedNode);
                        }

                        insertedNode.Parent.Red = false;
                        insertedNode.Parent.Parent.Red = true;
                        RotateLeft(insertedNode.Parent.Parent);
                    }
                }
            }

            _root.Red = false;
        }



        private readonly IComparer<TKey> _comparer;
        private int _count;
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
            public TValue Value;
            public TKey Key;
            public bool Red;
            public RedBlackNode Left;
            public RedBlackNode Right;
            public RedBlackNode Parent;

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

        }
        #endregion
    }
}