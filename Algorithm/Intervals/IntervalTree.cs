using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Intervals
{
    [Obsolete("IN DEVELOPMENT")]
    internal class IntervalTree<TPoint, TValue> : IIntervalTree<TPoint, TValue>
    {
        private IntervalNode _root;
        private ulong _modifications;
        private readonly IComparer<IntervalPoint<TPoint>> _comparer;

        public IntervalPoint<TPoint> MaxEndPoint
        {
            get
            {
                if (_root == null)
                    throw new InvalidOperationException("Cannot determine max end point for empty interval tree");
                return _root.MaxEndPoint;
            }
        }

        public IntervalPoint<TPoint> MinEndPoint
        {
            get
            {
                if (_root == null)
                    throw new InvalidOperationException("Cannot determine min end point for empty interval tree");
                return _root.MinEndPoint;
            }
        }
        public bool IsSynchronized => false;
        public bool IsReadOnly => false;
        public Object SyncRoot { get; }
        public int Count { get; private set; }

        public IntervalTree(IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> intervals, IComparer<IntervalPoint<TPoint>> comparer = null) : this(comparer)
        {
            this.AddRange(intervals);
        }

        public IntervalTree(IComparer<IntervalPoint<TPoint>> comparer = null)
        {
            _comparer = comparer ?? IntervalPointComparer<TPoint>.Default;
            SyncRoot = new object();
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            return new IntervalTreeEnumerator(this);
        }

        public IEnumerator<KeyValuePair<Interval<TPoint>, TValue>> GetEnumerator()
        {
            return new IntervalTreeEnumerator(this);
        }

        public void CopyTo(
            Array array,
            int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            PerformCopy(arrayIndex, array.Length, (i, v) => array.SetValue(v, i));
        }

        public bool Remove(KeyValuePair<Interval<TPoint>, TValue> item)
        {
            return Remove(item.Key);
        }



        public void CopyTo(
            KeyValuePair<Interval<TPoint>, TValue>[] array,
            int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            PerformCopy(arrayIndex, array.Length, (i, v) => array[i] = v);
        }

        public bool Contains(KeyValuePair<Interval<TPoint>, TValue> item)
        {
            return FindMatchingNodes(item.Key).Any();
        }

        public void Clear()
        {
            SetRoot(null);
            Count = 0;
            _modifications++;
        }

        public void Add(KeyValuePair<Interval<TPoint>, TValue> item)
        {
            var newNode = new IntervalNode(item, _comparer);

            if (_root == null)
            {
                SetRoot(newNode);
                Count = 1;
                _modifications++;
                return;
            }

            IntervalNode node = _root;
            while (true)
            {
                var startCmp = _comparer.Compare(newNode.Start, node.Start);
                if (startCmp <= 0)
                {
                    if (startCmp == 0)
                        throw new InvalidOperationException("Cannot add the same item twice (object reference already exists in db)");

                    if (node.Left == null)
                    {
                        node.Left = newNode;
                        break;
                    }
                    node = node.Left;
                }
                else
                {
                    if (node.Right == null)
                    {
                        node.Right = newNode;
                        break;
                    }
                    node = node.Right;
                }
            }

            _modifications++;
            Count++;

            // Restructure tree to be balanced
            node = newNode;
            while (node != null)
            {
                node.UpdateHeight();
                node.UpdateCachedPoints();
                Rebalance(node);
                node = node.Parent;
            }
        }

        /// <summary>
        /// Removes an item.
        /// </summary>
        /// <param name="item">The item to remove</param>
        /// <returns>True if an item was removed</returns>
        /// <remarks>
        /// This method uses the collection’s objects’ Equals and CompareTo methods on item to retrieve the existing item.
        /// </remarks>
        public bool Remove(Interval<TPoint> item)
        {

            if (_root == null)
                return false;

            var candidates = FindMatchingNodes(item).ToList();

            if (candidates.Count == 0)
                return false;

            IntervalNode toBeRemoved = candidates[0];

            var parent = toBeRemoved.Parent;
            var isLeftChild = toBeRemoved.IsLeftChild;

            if (toBeRemoved.Left == null && toBeRemoved.Right == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = null;
                    else
                        parent.Right = null;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(null);
                }
            }
            else if (toBeRemoved.Right == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = toBeRemoved.Left;
                    else
                        parent.Right = toBeRemoved.Left;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(toBeRemoved.Left);
                }
            }
            else if (toBeRemoved.Left == null)
            {
                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = toBeRemoved.Right;
                    else
                        parent.Right = toBeRemoved.Right;

                    Rebalance(parent);
                }
                else
                {
                    SetRoot(toBeRemoved.Right);
                }
            }
            else
            {
                IntervalNode replacement, replacementParent, temp;

                if (toBeRemoved.Balance > 0)
                {
                    if (toBeRemoved.Left.Right == null)
                    {
                        replacement = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                        temp = replacement;
                    }
                    else
                    {
                        replacement = toBeRemoved.Left.Right;
                        while (replacement.Right != null)
                        {
                            replacement = replacement.Right;
                        }
                        replacementParent = replacement.Parent;
                        replacementParent.Right = replacement.Left;

                        temp = replacementParent;

                        replacement.Left = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                    }
                }
                else
                {
                    if (toBeRemoved.Right.Left == null)
                    {
                        replacement = toBeRemoved.Right;
                        replacement.Left = toBeRemoved.Left;
                        temp = replacement;
                    }
                    else
                    {
                        replacement = toBeRemoved.Right.Left;
                        while (replacement.Left != null)
                        {
                            replacement = replacement.Left;
                        }
                        replacementParent = replacement.Parent;
                        replacementParent.Left = replacement.Right;

                        temp = replacementParent;

                        replacement.Left = toBeRemoved.Left;
                        replacement.Right = toBeRemoved.Right;
                    }
                }

                if (parent != null)
                {
                    if (isLeftChild)
                        parent.Left = replacement;
                    else
                        parent.Right = replacement;
                }
                else
                {
                    SetRoot(replacement);
                }

                Rebalance(temp);
            }

            toBeRemoved.Parent = null;
            Count--;
            _modifications++;
            return true;
        }
        
        public IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> FindAt(IntervalPoint<TPoint> point)
        {
            return PerformStabbingQuery(_root, point).Select(node => node.Data);
        }

        public bool Contains(IntervalPoint<TPoint> point)
        {
            return FindAt(point).Any();
        }

        public bool Overlaps(Interval<TPoint> interval)
        {
            return PerformStabbingQuery(_root, interval).Any();
        }

        public IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> FindOverlaps(Interval<TPoint> interval)
        {
            return PerformStabbingQuery(_root, interval).Select(node => node.Data);
        }
        
        private void PerformCopy(int arrayIndex, int arrayLength, Action<int, KeyValuePair<Interval<TPoint>, TValue>> setAtIndexDelegate)
        {
            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            int i = arrayIndex;
            using IEnumerator<KeyValuePair<Interval<TPoint>, TValue>> enumerator = GetEnumerator();
            while (enumerator.MoveNext())
            {
                if (i >= arrayLength)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Not enough elements in array to copy content into");
                setAtIndexDelegate(i, enumerator.Current);
                i++;
            }
        }

        private IEnumerable<IntervalNode> FindMatchingNodes(Interval<TPoint> interval)
        {
            return PerformStabbingQuery(_root, interval).Where(node => node.Data.Key.Equals(interval));
        }

        private void SetRoot(IntervalNode node)
        {
            _root = node;
            if (_root != null)
                _root.Parent = null;
        }

        private IEnumerable<IntervalNode> PerformStabbingQuery(IntervalNode node, IntervalPoint<TPoint> point)
        {
            if (node == null)
                yield break;
            if (new IntervalGougedPointComparer<TPoint>(_comparer, false).Compare(point, node.MaxEndPoint) > 0)
                yield break;

            if (node.Left != null)
                foreach (var n in PerformStabbingQuery(node.Left, point))
                    yield return n;

            if (node.Data.Key.Contains(point))
                yield return node;

            if (new IntervalGougedPointComparer<TPoint>(_comparer, true).Compare(point, node.Start) < 0)
                yield break;

            if (node.Right != null)
                foreach (var n in PerformStabbingQuery(node.Right, point))
                    yield return n;
        }

        private IEnumerable<IntervalNode> PerformStabbingQuery(IntervalNode node, Interval<TPoint> interval)
        {
            if (node == null)
                yield break;

            if (new IntervalGougedPointComparer<TPoint>(_comparer, true).Compare(interval.StartPoint, node.MaxEndPoint) > 0)
                yield break;

            if (node.Left != null)
                foreach (var n in PerformStabbingQuery(node.Left, interval))
                    yield return n;

            if (node.Data.Key.Overlaps(interval))
                yield return node;

            if (new IntervalGougedPointComparer<TPoint>(_comparer, true).Compare(interval.EndPoint, node.Start) < 0)
                yield break;

            if (node.Right != null)
                foreach (var n in PerformStabbingQuery(node.Right, interval))
                    yield return n;
        }

        private void Rebalance(IntervalNode node)
        {
            if (node.Balance > 1)
            {
                if (node.Left.Balance < 0)
                    RotateLeft(node.Left);
                RotateRight(node);
            }
            else if (node.Balance < -1)
            {
                if (node.Right.Balance > 0)
                    RotateRight(node.Right);
                RotateLeft(node);
            }
        }

        private void RotateLeft(IntervalNode node)
        {
            var parent = node.Parent;
            var isNodeLeftChild = node.IsLeftChild;

            // Make node.Right the new root of this sub tree (instead of node)
            var pivotNode = node.Right;
            node.Right = pivotNode.Left;
            pivotNode.Left = node;

            if (parent != null)
            {
                if (isNodeLeftChild)
                    parent.Left = pivotNode;
                else
                    parent.Right = pivotNode;
            }
            else
            {
                SetRoot(pivotNode);
            }
        }

        private void RotateRight(IntervalNode node)
        {
            var parent = node.Parent;
            var isNodeLeftChild = node.IsLeftChild;

            // Make node.Left the new root of this sub tree (instead of node)
            var pivotNode = node.Left;
            node.Left = pivotNode.Right;
            pivotNode.Right = node;

            if (parent != null)
            {
                if (isNodeLeftChild)
                    parent.Left = pivotNode;
                else
                    parent.Right = pivotNode;
            }
            else
            {
                SetRoot(pivotNode);
            }
        }
        

        #region Inner classes

        private sealed class IntervalNode
        {
            private readonly IComparer<IntervalPoint<TPoint>> _comparer;
            private IntervalNode _left;
            private IntervalNode _right;
            public IntervalNode Parent { get; set; }
            public IntervalPoint<TPoint> Start => Data.Key.StartPoint;
            private IntervalPoint<TPoint> End => Data.Key.EndPoint;
            public KeyValuePair<Interval<TPoint>, TValue> Data { get; private set; }
            private int Height { get; set; }
            public IntervalPoint<TPoint> MaxEndPoint { get; private set; }
            public IntervalPoint<TPoint> MinEndPoint { get; private set; }
            public IntervalNode(KeyValuePair<Interval<TPoint>, TValue> data, IComparer<IntervalPoint<TPoint>> comparer)
            {
                _comparer = comparer;
                Data = data;
                UpdateCachedPoints();
            }

            public IntervalNode Left
            {
                get => _left;
                set
                {
                    _left = value;
                    if (_left != null)
                        _left.Parent = this;
                    UpdateHeight();
                    UpdateCachedPoints();
                }
            }

            public IntervalNode Right
            {
                get => _right;
                set
                {
                    _right = value;
                    if (_right != null)
                        _right.Parent = this;
                    UpdateHeight();
                    UpdateCachedPoints();
                }
            }

            public int Balance
            {
                get
                {
                    if (Left != null && Right != null)
                        return Left.Height - Right.Height;
                    if (Left != null)
                        return Left.Height + 1;
                    if (Right != null)
                        return -(Right.Height + 1);
                    return 0;
                }
            }

            public bool IsLeftChild => Parent != null && Parent.Left == this;



            public void UpdateHeight()
            {
                if (Left != null && Right != null)
                    Height = Math.Max(Left.Height, Right.Height) + 1;
                else if (Left != null)
                    Height = Left.Height + 1;
                else if (Right != null)
                    Height = Right.Height + 1;
                else
                    Height = 0;
            }

            public void UpdateCachedPoints()
            {
                var max = End;
                if (Left != null)
                    max = Interval.Max(max, Left.MaxEndPoint, false, _comparer);
                if (Right != null)
                    max = Interval.Max(max, Right.MaxEndPoint, false, _comparer);
                MaxEndPoint = max;

                var min = Start;
                if (Left != null)
                    min = Interval.Min(min, Left.MinEndPoint, true, _comparer);
                if (Right != null)
                    min = Interval.Max(min, Right.MinEndPoint, true, _comparer);
                MinEndPoint = min;
            }

            public override string ToString()
            {
                return string.Format("[{0},{1}], maxEnd={2}, minEnd={3}", Start, End, MaxEndPoint, MinEndPoint);
            }
        }

        private sealed class IntervalTreeEnumerator : IEnumerator<KeyValuePair<Interval<TPoint>, TValue>>
        {
            private readonly ulong _modificationsAtCreation;
            private readonly IntervalTree<TPoint, TValue> _tree;
            private readonly IntervalNode _startNode;
            private IntervalNode _current;
            private bool _hasVisitedCurrent;
            private bool _hasVisitedRight;

            public IntervalTreeEnumerator(IntervalTree<TPoint, TValue> tree)
            {
                this._tree = tree;
                _modificationsAtCreation = tree._modifications;
                _startNode = GetLeftMostDescendantOrSelf(tree._root);
                Reset();
            }

            public KeyValuePair<Interval<TPoint>, TValue> Current
            {
                get
                {
                    if (_current == null)
                        throw new InvalidOperationException("Enumeration has finished.");

                    if (ReferenceEquals(_current, _startNode) && !_hasVisitedCurrent)
                        throw new InvalidOperationException("Enumeration has not started.");

                    return _current.Data;
                }
            }

            object IEnumerator.Current => Current;

            public void Reset()
            {
                if (_modificationsAtCreation != _tree._modifications)
                    throw new InvalidOperationException("Collection was modified.");
                _current = _startNode;
                _hasVisitedCurrent = false;
                _hasVisitedRight = false;
            }

            public bool MoveNext()
            {
                if (_modificationsAtCreation != _tree._modifications)
                    throw new InvalidOperationException("Collection was modified.");

                if (_tree._root == null)
                    return false;

                // Visit this node
                if (!_hasVisitedCurrent)
                {
                    _hasVisitedCurrent = true;
                    return true;
                }

                // Go right, visit the right's left most descendant (or the right node itself)
                if (!_hasVisitedRight && _current.Right != null)
                {
                    _current = _current.Right;
                    MoveToLeftMostDescendant();
                    _hasVisitedCurrent = true;
                    _hasVisitedRight = false;
                    return true;
                }

                // Move upward
                do
                {
                    var wasVisitingFromLeft = _current.IsLeftChild;
                    _current = _current.Parent;
                    if (wasVisitingFromLeft)
                    {
                        _hasVisitedCurrent = false;
                        _hasVisitedRight = false;
                        return MoveNext();
                    }
                } while (_current != null);

                return false;
            }

            private void MoveToLeftMostDescendant()
            {
                _current = GetLeftMostDescendantOrSelf(_current);
            }

            private IntervalNode GetLeftMostDescendantOrSelf(IntervalNode node)
            {
                if (node == null)
                    return null;
                while (node.Left != null)
                {
                    node = node.Left;
                }
                return node;
            }

            public void Dispose()
            {
            }
        }

        #endregion
    }
}
