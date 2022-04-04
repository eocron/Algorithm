using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Eocron.Algorithms.Tree
{
    public sealed class RedBlackTree<TKey, TValue> : IRedBlackTree<TKey, TValue>
    {
        private readonly IComparer<TKey> _comparer;
        private Node _root;
        private int _count;
        public int Count => _count;
        public ICollection<TKey> Keys => TraverseNodes().Select(x => x.Key).ToList();
        public ICollection<TValue> Values => TraverseNodes().Select(x => x.Value).ToList();
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => TraverseNodes().Select(x => x.Value);
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => TraverseNodes().Select(x => x.Key);
        public bool IsReadOnly => false;

        public RedBlackTree(IComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public RedBlackTree(IEnumerable<KeyValuePair<TKey, TValue>> items, IComparer<TKey> comparer = null) : this(comparer)
        {
            foreach (var keyValuePair in items)
            {
                Add(keyValuePair);
            }
        }

        public KeyValuePair<TKey, TValue> MinKeyValue()
        {
            if (_root == null)
                throw new ArgumentException("Tree is empty.");
            var node = MinValueNode(_root);
            return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
        }

        public KeyValuePair<TKey, TValue> MaxKeyValue()
        {
            if (_root == null)
                throw new ArgumentException("Tree is empty.");
            var node = MaxValueNode(_root);
            return new KeyValuePair<TKey, TValue>(node.Key, node.Value);
        }

        public bool Remove(TKey key)
        {
            var node = RemoveBst(_root, key);
            FixDelete(node);
            if (node != null)
                _count--;
            return node != null;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var node in TraverseNodes())
            {
                array[arrayIndex++] = new KeyValuePair<TKey, TValue>(node.Key, node.Value);
            }
        }

        public bool Remove(KeyValuePair<TKey, TValue> keyValuePair)
        {
            return Remove(keyValuePair.Key);
        }

        public void Add(TKey key, TValue value)
        {
            Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void Add(KeyValuePair<TKey, TValue> keyValuePair)
        {
            var node = new Node {Key = keyValuePair.Key, Value = keyValuePair.Value};
            _root = AddBst(_root, node);
            FixInsert(node);
            _count++;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Node node;
            if (TryFindNode(key, out node))
            {
                value = node.Value;
                return true;
            }
            value = default;
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                Node node;
                if (TryFindNode(key, out node))
                    return node.Value;
                throw new KeyNotFoundException();
            }
            set
            {
                Node node;
                if (TryFindNode(key, out node))
                    node.Value = value;
                else
                {
                    Add(key, value);
                }
            }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return TraverseNodes().Select(x=> new KeyValuePair<TKey,TValue>(x.Key, x.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool ContainsKey(TKey key)
        {
            var node = _root;
            while (node != null)
            {
                var cmp = _comparer.Compare(key, node.Key);
                if (cmp == 0)
                    return true;
                if (cmp < 0)
                    node = node.Left;
                if (cmp > 0)
                    node = node.Right;
            }
            return false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> keyValuePair)
        {
            return ContainsKey(keyValuePair.Key);
        }

        public void Clear()
        {
            _root = null;
            _count = 0;
        }

        private IEnumerable<Node> TraverseNodes()
        {
            if (_root == null)
                yield break;

            var stack = new Stack<Node>(_count);
            var iter = _root;
            while (iter != null || stack.Count > 0)
            {
                while (iter != null)
                {
                    stack.Push(iter);
                    iter = iter.Left;
                }
                iter = stack.Pop();
                yield return iter;
                iter = iter.Right;
            }
        }

        private bool TryFindNode(TKey key, out Node found)
        {
            var node = _root;
            while (node != null)
            {
                var cmp = _comparer.Compare(key, node.Key);
                if (cmp == 0)
                {
                    found = node;
                    return true;
                }

                if (cmp < 0)
                    node = node.Left;
                if (cmp > 0)
                    node = node.Right;
            }

            found = default;
            return false;
        }

        private static NodeColor GetColor(Node node)
        {
            if (node == null)
                return NodeColor.Black;

            return node.Color;
        }

        private static void SetColor(Node node, NodeColor color)
        {
            if (node == null)
                return;

            node.Color = color;
        }

        private Node AddBst(Node parent, Node node)
        {
            if (parent == null)
                return node;

            var cmp = _comparer.Compare(node.Key, parent.Key);
            if (cmp < 0)
            {
                parent.Left = AddBst(parent.Left, node);
                parent.Left.Parent = parent;
            }
            else if (cmp > 0)
            {
                parent.Right = AddBst(parent.Right, node);
                parent.Right.Parent = parent;
            }
            else
            {
                throw new ArgumentException("Key already exist.");
            }
            return parent;
        }

        private void RotateLeft(Node node)
        {
            var rightChild = node.Right;
            node.Right = rightChild.Left;

            if (node.Right != null)
                node.Right.Parent = node;

            rightChild.Parent = node.Parent;

            if (node.Parent == null)
                _root = rightChild;
            else if (node == node.Parent.Left)
                node.Parent.Left = rightChild;
            else
                node.Parent.Right = rightChild;

            rightChild.Left = node;
            node.Parent = rightChild;
        }

        private void RotateRight(Node node)
        {
            var leftChild = node.Left;
            node.Left = leftChild.Right;

            if (node.Left != null)
                node.Left.Parent = node;

            leftChild.Parent = node.Parent;

            if (node.Parent == null)
                _root = leftChild;
            else if (node == node.Parent.Left)
                node.Parent.Left = leftChild;
            else
                node.Parent.Right = leftChild;

            leftChild.Right = node;
            node.Parent = leftChild;
        }

        private void FixInsert(Node node)
        {
            while (node != _root && GetColor(node) == NodeColor.Red && GetColor(node.Parent) == NodeColor.Red)
            {
                var parent = node.Parent;
                var grandparent = parent.Parent;
                if (parent == grandparent.Left)
                {
                    var uncle = grandparent.Right;
                    if (GetColor(uncle) == NodeColor.Red)
                    {
                        SetColor(uncle, NodeColor.Black);
                        SetColor(parent, NodeColor.Black);
                        SetColor(grandparent, NodeColor.Red);
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Right)
                        {
                            RotateLeft(parent);
                            node = parent;
                            parent = node.Parent;
                        }

                        RotateRight(grandparent);
                        var tmp = parent.Color;
                        parent.Color = grandparent.Color;
                        grandparent.Color = tmp;
                        node = parent;
                    }
                }
                else
                {
                    var uncle = grandparent.Left;
                    if (GetColor(uncle) == NodeColor.Red)
                    {
                        SetColor(uncle, NodeColor.Black);
                        SetColor(parent, NodeColor.Black);
                        SetColor(grandparent, NodeColor.Red);
                        node = grandparent;
                    }
                    else
                    {
                        if (node == parent.Left)
                        {
                            RotateRight(parent);
                            node = parent;
                            parent = node.Parent;
                        }

                        RotateLeft(grandparent);
                        var tmp = parent.Color;
                        parent.Color = grandparent.Color;
                        grandparent.Color = tmp;
                        node = parent;
                    }
                }
            }

            SetColor(_root, NodeColor.Black);
        }

        private void FixDelete(Node node)
        {
            if (node == null)
                return;

            if (node == _root)
            {
                _root = null;
                return;
            }

            if (GetColor(node) == NodeColor.Red || GetColor(node.Left) == NodeColor.Red ||
                GetColor(node.Right) == NodeColor.Red)
            {
                var child = node.Left ?? node.Right;

                if (node == node.Parent.Left)
                {
                    node.Parent.Left = child;
                    if (child != null)
                        child.Parent = node.Parent;
                    SetColor(child, NodeColor.Black);
                }
                else
                {
                    node.Parent.Right = child;
                    if (child != null)
                        child.Parent = node.Parent;
                    SetColor(child, NodeColor.Black);
                }
            }
            else
            {
                var ptr = node;
                SetColor(ptr, NodeColor.DoubleBlack);
                while (ptr != _root && GetColor(ptr) == NodeColor.DoubleBlack)
                {
                    var parent = ptr.Parent;
                    Node sibling;
                    if (ptr == parent.Left)
                    {
                        sibling = parent.Right;
                        if (GetColor(sibling) == NodeColor.Red)
                        {
                            SetColor(sibling, NodeColor.Black);
                            SetColor(parent, NodeColor.Red);
                            RotateLeft(parent);
                        }
                        else
                        {
                            if (GetColor(sibling.Left) == NodeColor.Black && GetColor(sibling.Right) == NodeColor.Black)
                            {
                                SetColor(sibling, NodeColor.Red);
                                if (GetColor(parent) == NodeColor.Red)
                                    SetColor(parent, NodeColor.Black);
                                else
                                    SetColor(parent, NodeColor.DoubleBlack);
                                ptr = parent;
                            }
                            else
                            {
                                if (GetColor(sibling.Right) == NodeColor.Black)
                                {
                                    SetColor(sibling.Left, NodeColor.Black);
                                    SetColor(sibling, NodeColor.Red);
                                    RotateRight(sibling);
                                    sibling = parent.Right;
                                }

                                SetColor(sibling, parent.Color);
                                SetColor(parent, NodeColor.Black);
                                SetColor(sibling.Right, NodeColor.Black);
                                RotateLeft(parent);
                                break;
                            }
                        }
                    }
                    else
                    {
                        sibling = parent.Left;
                        if (GetColor(sibling) == NodeColor.Red)
                        {
                            SetColor(sibling, NodeColor.Black);
                            SetColor(parent, NodeColor.Red);
                            RotateRight(parent);
                        }
                        else
                        {
                            if (GetColor(sibling.Left) == NodeColor.Black && GetColor(sibling.Right) == NodeColor.Black)
                            {
                                SetColor(sibling, NodeColor.Red);
                                if (GetColor(parent) == NodeColor.Red)
                                    SetColor(parent, NodeColor.Black);
                                else
                                    SetColor(parent, NodeColor.DoubleBlack);
                                ptr = parent;
                            }
                            else
                            {
                                if (GetColor(sibling.Left) == NodeColor.Black)
                                {
                                    SetColor(sibling.Right, NodeColor.Black);
                                    SetColor(sibling, NodeColor.Red);
                                    RotateLeft(sibling);
                                    sibling = parent.Left;
                                }

                                SetColor(sibling, parent.Color);
                                SetColor(parent, NodeColor.Black);
                                SetColor(sibling.Left, NodeColor.Black);
                                RotateRight(parent);
                                break;
                            }
                        }
                    }
                }

                if (node == node.Parent.Left)
                    node.Parent.Left = null;
                else
                    node.Parent.Right = null;
                SetColor(_root, NodeColor.Black);
            }
        }

        private Node RemoveBst(Node root, TKey key)
        {
            if (root == null)
                return null;

            var cmp = _comparer.Compare(key, root.Key);
            if (cmp < 0)
                return RemoveBst(root.Left, key);

            if (cmp > 0)
                return RemoveBst(root.Right, key);

            if (root.Left == null || root.Right == null)
                return root;

            var temp = MinValueNode(root.Right);
            root.Key = temp.Key;
            root.Value = temp.Value;
            return RemoveBst(root.Right, temp.Key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Node MinValueNode(Node node)
        {

            var ptr = node;

            while (ptr.Left != null)
                ptr = ptr.Left;

            return ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Node MaxValueNode(Node node)
        {
            var ptr = node;

            while (ptr.Right != null)
                ptr = ptr.Right;

            return ptr;
        }

        private enum NodeColor
        {
            Black,
            Red,
            DoubleBlack
        }

        private class Node
        {
            public TKey Key;
            public TValue Value;
            public NodeColor Color;
            public Node Left;
            public Node Right;
            public Node Parent;
        }
    }
}
