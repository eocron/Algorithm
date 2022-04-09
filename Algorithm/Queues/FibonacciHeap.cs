using System;
using System.Collections.Generic;
using System.Linq;

namespace Eocron.Algorithms.Queues
{
    public class FibonacciHeap<TPriority, TValue> : IPriorityQueue<TPriority, TValue>
    {
        private const double OneOverLogPhi = 2.07808692123503d;//1d / Math.Log((1d + Math.Sqrt(5d)) / 2d);
        private readonly IComparer<TPriority> _comparer;

        private readonly Dictionary<TPriority, ISet<FibonacciHeapNode>> _nodeIndex = new Dictionary<TPriority, ISet<FibonacciHeapNode>>();

        private FibonacciHeapNode _minNode;

        public FibonacciHeap(IComparer<TPriority> comparer = null)
        {
            _comparer = comparer ?? Comparer<TPriority>.Default;
        }

        public int Count { get; private set; }

        public void Clear()
        {
            _minNode = null;
            _nodeIndex.Clear();
            Count = 0;
        }

        public void EnqueueOrUpdate(
            KeyValuePair<TPriority, TValue> item,
            Func<KeyValuePair<TPriority, TValue>, KeyValuePair<TPriority, TValue>> onUpdate)
        {
            var found = FindAllInIndex(item.Key).Select(x => new
            {
                next = onUpdate(new KeyValuePair<TPriority, TValue>(x.Priority, x.Value)),
                prev = x
            }).ToList();
            if (found.Count > 0)
            {
                foreach (var f in found)
                    if (_comparer.Compare(f.next.Key, f.prev.Priority) > 0)
                        throw new NotSupportedException("Setting larger priority is not implemented.");

                foreach (var f in found)
                {
                    RemoveFromIndex(f.prev);
                    f.prev.Priority = f.next.Key;
                    f.prev.Value = f.next.Value;
                    AddInIndex(f.prev);
                    UpdateLinks(f.prev);
                }
            }
            else
            {
                Enqueue(item);
            }
        }

        public KeyValuePair<TPriority, TValue> Dequeue()
        {
            if (Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            var node = RemoveMin();
            RemoveFromIndex(node);
            return new KeyValuePair<TPriority, TValue>(node.Priority, node.Value);
        }

        public void Enqueue(KeyValuePair<TPriority, TValue> item)
        {
            var node = new FibonacciHeapNode(item.Value, item.Key);

            if (_minNode != null)
            {
                node.Left = _minNode;
                node.Right = _minNode.Right;
                _minNode.Right = node;
                node.Right.Left = node;

                if (_comparer.Compare(node.Priority, _minNode.Priority) < 0) _minNode = node;
            }
            else
            {
                _minNode = node;
            }

            Count++;

            AddInIndex(node);
        }

        public KeyValuePair<TPriority, TValue> Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("Queue is empty.");
            return new KeyValuePair<TPriority, TValue>(_minNode.Priority, _minNode.Value);
        }

        private void AddInIndex(FibonacciHeapNode node)
        {
            if (!_nodeIndex.TryGetValue(node.Priority, out var set))
            {
                set = new HashSet<FibonacciHeapNode>();
                _nodeIndex.Add(node.Priority, set);
            }

            set.Add(node);
        }

        private void RemoveFromIndex(FibonacciHeapNode node)
        {
            var set = _nodeIndex[node.Priority];
            set.Remove(node);
            if (set.Count == 0)
                _nodeIndex.Remove(node.Priority);
        }

        private void UpdateLinks(FibonacciHeapNode x)
        {
            var y = x.Parent;

            if (y != null && _comparer.Compare(x.Priority, y.Priority) < 0)
            {
                Cut(x, y);
                CascadingCut(y);
            }

            if (_comparer.Compare(x.Priority, _minNode.Priority) < 0) _minNode = x;
        }

        private IEnumerable<FibonacciHeapNode> FindAllInIndex(TPriority key)
        {
            if (_nodeIndex.TryGetValue(key, out var tmp))
                return tmp;
            return Array.Empty<FibonacciHeapNode>();
        }

        private FibonacciHeapNode RemoveMin()
        {
            var minNode = _minNode;

            if (minNode != null)
            {
                var numKids = minNode.Degree;
                var oldMinChild = minNode.Child;
                while (numKids > 0)
                {
                    var tempRight = oldMinChild.Right;
                    oldMinChild.Left.Right = oldMinChild.Right;
                    oldMinChild.Right.Left = oldMinChild.Left;
                    oldMinChild.Left = _minNode;
                    oldMinChild.Right = _minNode.Right;
                    _minNode.Right = oldMinChild;
                    oldMinChild.Right.Left = oldMinChild;
                    oldMinChild.Parent = null;
                    oldMinChild = tempRight;
                    numKids--;
                }

                minNode.Left.Right = minNode.Right;
                minNode.Right.Left = minNode.Left;

                if (minNode == minNode.Right)
                {
                    _minNode = null;
                }
                else
                {
                    _minNode = minNode.Right;
                    Consolidate();
                }
                Count--;
            }

            return minNode;
        }


        private void CascadingCut(FibonacciHeapNode y)
        {
            var z = y.Parent;
            if (z != null)
            {
                if (!y.Mark)
                {
                    y.Mark = true;
                }
                else
                {
                    Cut(y, z);


                    CascadingCut(z);
                }
            }
        }

        private void Consolidate()
        {
            var arraySize = (int) Math.Floor(Math.Log(Count) * OneOverLogPhi) + 1;

            var array = new List<FibonacciHeapNode>(arraySize);
            for (var i = 0; i < arraySize; i++) array.Add(null);
            var numRoots = 0;
            var x = _minNode;
            if (x == null)
                return;

            numRoots++;
            x = x.Right;

            while (x != _minNode)
            {
                numRoots++;
                x = x.Right;
            }

            while (numRoots > 0)
            {
                var d = x.Degree;
                var next = x.Right;
                while (true)
                {
                    var y = array[d];
                    if (y == null)
                        break;

                    if (_comparer.Compare(x.Priority, y.Priority) > 0)
                    {
                        var temp = y;
                        y = x;
                        x = temp;
                    }

                    Link(y, x);
                    array[d] = null;
                    d++;
                }

                array[d] = x;
                x = next;
                numRoots--;
            }

            _minNode = null;

            for (var i = 0; i < arraySize; i++)
            {
                var y = array[i];
                if (y == null) continue;
                if (_minNode != null)
                {
                    y.Left.Right = y.Right;
                    y.Right.Left = y.Left;
                    y.Left = _minNode;
                    y.Right = _minNode.Right;
                    _minNode.Right = y;
                    y.Right.Left = y;

                    if (_comparer.Compare(y.Priority, _minNode.Priority) < 0)
                        _minNode = y;
                }
                else
                {
                    _minNode = y;
                }
            }
        }


        private void Cut(FibonacciHeapNode x, FibonacciHeapNode y)
        {
            x.Left.Right = x.Right;
            x.Right.Left = x.Left;
            y.Degree--;
            if (y.Child == x) 
                y.Child = x.Right;
            if (y.Degree == 0) 
                y.Child = null;
            x.Left = _minNode;
            x.Right = _minNode.Right;
            _minNode.Right = x;
            x.Right.Left = x;
            x.Parent = null;
            x.Mark = false;
        }


        private static void Link(FibonacciHeapNode newChild, FibonacciHeapNode newParent)
        {
            newChild.Left.Right = newChild.Right;
            newChild.Right.Left = newChild.Left;
            newChild.Parent = newParent;

            if (newParent.Child == null)
            {
                newParent.Child = newChild;
                newChild.Right = newChild;
                newChild.Left = newChild;
            }
            else
            {
                newChild.Left = newParent.Child;
                newChild.Right = newParent.Child.Right;
                newParent.Child.Right = newChild;
                newChild.Right.Left = newChild;
            }
            newParent.Degree++;
            newChild.Mark = false;
        }

        private class FibonacciHeapNode
        {
            public TPriority Priority;
            public TValue Value;
            public FibonacciHeapNode Child;
            public FibonacciHeapNode Left;
            public FibonacciHeapNode Parent;
            public FibonacciHeapNode Right;
            public bool Mark;
            public int Degree;

            public FibonacciHeapNode(TValue data, TPriority key)
            {
                Right = this;
                Left = this;
                Value = data;
                Priority = key;
            }
        }
    }
}