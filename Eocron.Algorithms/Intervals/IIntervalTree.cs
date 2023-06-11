﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace Eocron.Algorithms.Intervals
{
    [Obsolete("IN DEVELOPMENT")]
    internal interface IIntervalTree<TPoint, TValue> : ICollection<KeyValuePair<Interval<TPoint>, TValue>>, ICollection
    {
        /// <summary>
        /// Returns the maximum end point in the entire collection.
        /// </summary>
        IntervalPoint<TPoint> MaxEndPoint { get; }
        IntervalPoint<TPoint> MinEndPoint { get; }
        bool Remove(Interval<TPoint> interval);
        IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> FindAt(IntervalPoint<TPoint> point);
        bool Contains(IntervalPoint<TPoint> point);
        bool Overlaps(Interval<TPoint> interval);
        IEnumerable<KeyValuePair<Interval<TPoint>, TValue>> FindOverlaps(Interval<TPoint> interval);
    }
}