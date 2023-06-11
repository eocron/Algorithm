using System;
using System.Collections.Generic;
using Eocron.Algorithms.Intervals;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public sealed class IntervalSerializationTests
    {
        [Test]
        public void Simple()
        {
            var intervals = new[] {Interval.Create(1, 2), Interval.Create((int?) 3, null),};
            var json = JsonConvert.SerializeObject(intervals, Formatting.Indented);
            Console.WriteLine(json);
            var actual = JsonConvert.DeserializeObject<List<Interval<int>>>(json);
            CollectionAssert.AreEqual(intervals, actual);
        }
    }
}
