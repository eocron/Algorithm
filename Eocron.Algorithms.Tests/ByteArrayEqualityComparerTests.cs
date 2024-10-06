using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eocron.Algorithms.EqualityComparers;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class ByteArrayEqualityComparerTests
    {
        private static readonly Random _rnd = new(42);
        private readonly ByteArrayEqualityComparer _cmp = new();

        [Test(Description =
            "Guarantee that random array self collision is lower than some percent if single byte is flipped")]
        [TestCase(2)]
        [TestCase(16)]
        [TestCase(63)]
        [TestCase(512)]
        [TestCase(10000)]
        public void GetHashSelfCollisionsRnd(int size)
        {
            ClassicAssert.LessOrEqual(CalculateCollisions(GetSlightlyDifferentRndArrays(size), _cmp), 0.1d);
        }

        [Test(Description =
            "Guarantee that zero array self collision is lower than some percent if single bit is flipped")]
        [TestCase(2)]
        [TestCase(16)]
        [TestCase(63)]
        [TestCase(512)]
        [TestCase(10000)]
        public void GetHashSelfCollisionsSparse(int size)
        {
            ClassicAssert.LessOrEqual(CalculateCollisions(GetSlightlyDifferentSparseArrays(size), _cmp, true), 0.1d);
        }

        [Test(Description = "Guarantee that completely random array collision is lower than some percent")]
        [TestCase(123, 1000000)]
        public void GetHashCollisionsRnd(int size, int count)
        {
            ClassicAssert.LessOrEqual(CalculateCollisions(GetCompletelyDifferentRndArrays(size, count), _cmp, true), 0.001d);
        }

        [Test]
        [TestCaseSource(nameof(GetAreEqualTests))]
        public void AreEqual(byte[] a, byte[] b)
        {
            ClassicAssert.IsTrue(_cmp.Equals(a, b));
            ClassicAssert.AreEqual(_cmp.GetHashCode(a), _cmp.GetHashCode(b));
        }

        [Test]
        [TestCaseSource(nameof(GetAreNotEqualTests))]
        public void AreNotEqual(byte[] a, byte[] b)
        {
            ClassicAssert.IsFalse(_cmp.Equals(a, b));
            ClassicAssert.AreNotEqual(_cmp.GetHashCode(a), _cmp.GetHashCode(b));
        }

        [Test]
        public void SubArraysAreEqual()
        {
            var a = new byte[40000];
            var b = new byte[40000];
            _rnd.NextBytes(a);
            Array.Copy(a, b, a.Length);
            for (var i = 1; i < 40000; i += 149)
            {
                var aa = new ArraySegment<byte>(a, 0, i);
                var bb = new ArraySegment<byte>(b, 0, i);
                ClassicAssert.IsTrue(_cmp.Equals(aa, bb));
                ClassicAssert.AreEqual(_cmp.GetHashCode(aa), _cmp.GetHashCode(bb));
            }

            for (var i = 1; i < 40000; i += 149)
            {
                var aa = new ArraySegment<byte>(a, i, 31);
                var bb = new ArraySegment<byte>(b, i, 31);
                ClassicAssert.IsTrue(_cmp.Equals(aa, bb));
                ClassicAssert.AreEqual(_cmp.GetHashCode(aa), _cmp.GetHashCode(bb));
            }
        }

        [Test]
        public void BufferEqual()
        {
            var a = new byte[8 * 1024];
            var b = new byte[8 * 1024];
            _rnd.NextBytes(a);
            Array.Copy(a, b, a.Length);
            for (var i = 1; i < 8 * 1024; i += 149)
            {
                var aa = new ArraySegment<byte>(a, 0, i);
                var bb = new ArraySegment<byte>(b, 0, i);
                ClassicAssert.IsTrue(_cmp.Equals(aa, bb), i.ToString());
                ClassicAssert.AreEqual(_cmp.GetHashCode(aa), _cmp.GetHashCode(bb), i.ToString());
            }
        }

        private static IEnumerable<byte[]> GetCompletelyDifferentRndArrays(int size, int count)
        {
            var rnd = new Random(size);
            var data = new byte[size];

            for (var i = 0; i < count; i++)
            {
                rnd.NextBytes(data);
                yield return data;
            }
        }


        private static IEnumerable<byte[]> GetSlightlyDifferentRndArrays(int size)
        {
            var rnd = new Random(size);
            var data = new byte[size];
            rnd.NextBytes(data);
            for (var i = 0; i < size; i++)
            for (var j = 0; j < 8; j++)
            {
                var tmp = data[i];
                data[i] = (byte)rnd.Next();
                yield return data;
                data[i] = tmp;
            }
        }

        private static IEnumerable<byte[]> GetSlightlyDifferentSparseArrays(int size)
        {
            var data = new byte[size];
            for (var i = 0; i < size; i++)
            for (var j = 0; j < 8; j++)
            {
                data[i] = (byte)(data[i] | (1 << j));
                yield return data;
                data[i] = 0;
            }
        }

        private static float CalculateCollisions(IEnumerable<byte[]> datas, IEqualityComparer<byte[]> cmp,
            bool print = false)
        {
            var size = 0;
            var results = new List<Tuple<int, int>>();
            foreach (var data in datas)
            {
                results.Add(Tuple.Create(cmp.GetHashCode(data), size));
                size++;
            }

            var collisions = results
                .GroupBy(x => x.Item1)
                .Where(x => x.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            var collisionPercent = collisions.Count / (float)size;

            if (print)
            {
                var sb = new StringBuilder();
                sb.AppendFormat("Collision percent: {0:F8}%" + Environment.NewLine, 100f * collisionPercent);
                sb.AppendLine(string.Join("," + Environment.NewLine,
                    collisions
                        .OrderBy(x => x.Item1)
                        .ThenBy(x => x.Item2)
                        .Select(x => x.Item2 + "->" + x.Item1)));
                Console.WriteLine(sb);
            }

            return collisionPercent;
        }

        private static IEnumerable<TestCaseData> GetAreEqualTests()
        {
            for (var i = 1; i < 66; i++) yield return Eq(i).SetName($"+{i:00}b");
            yield return Eq(20 * 1024 * 1024).SetName("+20mb");
            yield return new TestCaseData(Array.Empty<byte>(), Array.Empty<byte>()).SetName("+empty");
            yield return new TestCaseData(null, null).SetName("+null");
        }

        private static IEnumerable<TestCaseData> GetAreNotEqualTests()
        {
            for (var i = 1; i < 66; i++) yield return NEq(i).SetName($"-{i:00}b");
            yield return new TestCaseData(GetBytes(20000), GetBytes(20000)).SetName("-20000b");
            yield return new TestCaseData(GetBytes(20 * 1024 * 1024), GetBytes(20 * 1024 * 1024)).SetName("-20mb");
            yield return new TestCaseData(Array.Empty<byte>(), null).SetName("-rnull");
            yield return new TestCaseData(null, Array.Empty<byte>()).SetName("-lnull");
            yield return new TestCaseData(new byte[] { 1, 2 }, new byte[] { 2 }).SetName("diff_size");
        }

        private static TestCaseData Eq(int size)
        {
            var a = GetBytes(size);
            var b = new byte[size];
            Array.Copy(a, b, size);
            return new TestCaseData(a, b);
        }

        private static TestCaseData NEq(int size)
        {
            var a = GetBytes(size);
            var b = new byte[size];
            Array.Copy(a, b, size);
            b[size - 1] = (byte)~b[size - 1];
            return new TestCaseData(a, b);
        }

        private static byte[] GetBytes(int size)
        {
            var res = new byte[size];
            _rnd.NextBytes(res);
            return res;
        }
    }
}