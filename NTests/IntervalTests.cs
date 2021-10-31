using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eocron.Algorithms.Intervals;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace NTests
{
    [TestFixture]
    public class IntervalTests
    {
        private static readonly IComparer<IntervalPoint<int>> Comparer =
            new IntervalPointComparer<int>(Comparer<int>.Default);

        private static readonly IComparer<IntervalPoint<int>> GougedComparer =
            new IntervalGougedPointComparer<int>(Comparer);
        private static IEnumerable<TestCaseData> GetGougedTests()
        {
            yield return new TestCaseData(1, false, 1, false, 0);
            yield return new TestCaseData(1, false, 2, false, -1);
            yield return new TestCaseData(2, false, 1, false, 1);
            yield return new TestCaseData(1, true, 1, false, -1).SetName("gouged should be lower if value equal");
            yield return new TestCaseData(1, false, 1, true, 1).SetName("not gouged should be greater if value equal");
            yield return new TestCaseData(2, true, 1, false, 1).SetName("gouged but greater by value");
        }

        
        private static IEnumerable<TestCaseData> GetIntersectTests()
        {
            var prefix = "intersect";
            yield return CreateSetTest("(0;1)", "(0;1)", prefix);
            yield return CreateSetTest("(0;1),(1;2)", "", prefix);
            yield return CreateSetTest("(0;1],(1;2)", "", prefix);
            yield return CreateSetTest("(0;1],[1;2)", "[1;1]", prefix);

            yield return CreateSetTest("(0;10],[5;20)", "[5;10]", prefix);
            yield return CreateSetTest("(0;10),[5;20)", "[5;10)", prefix);
            yield return CreateSetTest("(0;10],(5;20)", "(5;10]", prefix);
            yield return CreateSetTest("(0;10),(5;20)", "(5;10)", prefix);
        }

        private static IEnumerable<TestCaseData> GetUnionTests()
        {
            var prefix = "union";
            yield return CreateSetTest("(0;1)", "(0;1)", prefix);
            yield return CreateSetTest("(0;1),(1;2)", "(0;1),(1;2)", prefix);
            yield return CreateSetTest("(0;1],(1;2)", "(0;2)", prefix);
            yield return CreateSetTest("(0;1],[1;2)", "(0;2)", prefix);

            yield return CreateSetTest("(0;10],[5;20)", "(0;20)", prefix);
        }

        [Test]
        [TestCaseSource(nameof(GetGougedTests))]
        public void ComparerCheck(int a, bool gougedA, int b, bool gougedB, int expected)
        {
            var aa = new IntervalPoint<int>(a, gougedA);
            var bb = new IntervalPoint<int>(b, gougedB);
            Assert.AreEqual(expected, GougedComparer.Compare(aa, bb));
        }
        [Test]
        [TestCaseSource(nameof(GetIntersectTests))]
        public void IntersectCheck(List<Interval<int>> toIntersect, List<Interval<int>> expected)
        {
            var actual = new List<Interval<int>>();
            var intersection = IntervalHelpers.IntersectOrNull(toIntersect, Comparer);
            if(intersection != null)
                actual.Add(intersection.Value);
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        [TestCaseSource(nameof(GetUnionTests))]
        public void UnionCheck(List<Interval<int>> toUnite, List<Interval<int>> expected)
        {
            var union = IntervalHelpers.Union(toUnite, Comparer);
            CollectionAssert.AreEqual(expected, union);
        }

        private static Interval<int> ParseInterval(string input)
        {
            var match = Regex.Match(input,
                @"^\s*(?<gougeL>[\(\[])\s*(?<L>\-?\d+)\s*;\s*(?<R>\-?\d+)\s*(?<gougeR>[\)\]])\s*$");
            if (!match.Success)
                throw new ArgumentException("Invalid input.");

            return Interval<int>.Create(
                ParsePoint(match.Groups["L"].Value, match.Groups["gougeL"].Value == "("),
                ParsePoint(match.Groups["R"].Value, match.Groups["gougeR"].Value == ")"),
                new IntervalPointComparer<int>(Comparer<int>.Default));
        }

        private static List<Interval<int>> ParseIntervalSet(string input)
        {
            return input.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(ParseInterval).ToList();
        }

        private static TestCaseData CreateSetTest(string input, string result, string prefix)
        {
            return new TestCaseData(ParseIntervalSet(input), ParseIntervalSet(result)).SetName(prefix + ":" + input +
                " -> " + result);
        }

        private static IntervalPoint<int> ParsePoint(string input, bool gouge)
        {
            return new IntervalPoint<int>(int.Parse(input), gouge);
        }
    }
}
