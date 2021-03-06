﻿using Eocron.Algorithms;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NTests
{
    [TestFixture]
    public class ByteArrayEqualityComparerTests
    {
        private static Random _rnd = new Random(42);

        [Test]
        [TestCaseSource(nameof(GetAreEqualTests))]
        public void AreEqual(byte[] a, byte[] b)
        {
            var cmp = ByteArrayEqualityComparer.Default;
            Assert.IsTrue(cmp.Equals(a, b));
            Assert.AreEqual(cmp.GetHashCode(a), cmp.GetHashCode(b));
        }

        [Test]
        [TestCaseSource(nameof(GetAreNotEqualTests))]
        public void AreNotEqual(byte[] a, byte[] b)
        {
            var cmp = ByteArrayEqualityComparer.Default;
            Assert.IsFalse(cmp.Equals(a, b));
            Assert.AreNotEqual(cmp.GetHashCode(a), cmp.GetHashCode(b));
        }

        [Test]
        public void SubArraysAreEqual()
        {
            var a = new byte[40000];
            var b = new byte[40000];
            _rnd.NextBytes(a);
            Array.Copy(a, b, a.Length);
            var cmp = ByteArrayEqualityComparer.Default;
            for (int i = 1; i < 40000; i+=149)
            {
                var aa = new ArraySegment<byte>(a, 0, i);
                var bb = new ArraySegment<byte>(b, 0, i);
                Assert.IsTrue(cmp.Equals(aa, bb));
                Assert.AreEqual(cmp.GetHashCode(aa), cmp.GetHashCode(bb));
            }
        }

        [Test]
        public void BufferEqual()
        {
            var a = new byte[8 * 1024];
            var b = new byte[8 * 1024];
            _rnd.NextBytes(a);
            Array.Copy(a, b, a.Length);
            var cmp = ByteArrayEqualityComparer.Default;
            for (int i = 1; i < 8 * 1024; i += 149)
            {
                var aa = new ArraySegment<byte>(a, 0, i);
                var bb = new ArraySegment<byte>(b, 0, i);
                Assert.IsTrue(cmp.Equals(aa, bb));
                Assert.AreEqual(cmp.GetHashCode(aa), cmp.GetHashCode(bb));
            }
        }

        private static IEnumerable<TestCaseData> GetAreEqualTests()
        {
            yield return Eq(1).SetName("+1b");
            yield return Eq(20000).SetName("+20000b");
            yield return Eq(20 * 1024 * 1024).SetName("+20mb");
            yield return new TestCaseData(Array.Empty<byte>(), Array.Empty<byte>()).SetName("+empty");
            yield return new TestCaseData(null, null).SetName("+null");
        }
        private static IEnumerable<TestCaseData> GetAreNotEqualTests()
        {
            yield return new TestCaseData(new byte[] { 1 }, new byte[] { 2 }).SetName("-1b");
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
        private static byte[] GetBytes(int size)
        {
            var res = new byte[size];
            _rnd.NextBytes(res);
            return res;
        }
    }
}
