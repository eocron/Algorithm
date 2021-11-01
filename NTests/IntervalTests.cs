using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Eocron.Algorithms.Intervals;
using NUnit.Framework;

namespace NTests
{
    [TestFixture]
    public class IntervalTests
    {
        private static IEnumerable<TestCaseData> GetSetTests()
        {
            int counter = 1;
            var prefix = "union";
            yield return CreateSetTest("(0;1)", "(0;1)", prefix, ref counter);
            yield return CreateSetTest("(0;1),(1;2)", "(0;1),(1;2)", prefix, ref counter);
            yield return CreateSetTest("(0;1],(1;2)", "(0;2)", prefix, ref counter);
            yield return CreateSetTest("(0;1],[1;2)", "(0;2)", prefix, ref counter);
            yield return CreateSetTest("(0;10],[5;20)", "(0;20)", prefix, ref counter);

            prefix = "intersect";
            yield return CreateSetTest("(0;1)", "(0;1)", prefix, ref counter);
            yield return CreateSetTest("(0;1),(1;2)", "", prefix, ref counter);
            yield return CreateSetTest("(0;1],(1;2)", "", prefix, ref counter);
            yield return CreateSetTest("(0;1],[1;2)", "[1;1]", prefix, ref counter);
            yield return CreateSetTest("(1;2),[1;2)", "(1;2)", prefix, ref counter);
            yield return CreateSetTest("[1;2),(1;2)", "(1;2)", prefix, ref counter);
            yield return CreateSetTest("(0;10],[5;20)", "[5;10]", prefix, ref counter);
            yield return CreateSetTest("(0;10),[5;20)", "[5;10)", prefix, ref counter);
            yield return CreateSetTest("(0;10],(5;20)", "(5;10]", prefix, ref counter);
            yield return CreateSetTest("(0;10),(5;20)", "(5;10)", prefix, ref counter);

            prefix = "except";
            yield return CreateSetTest("(0;1),(1;2)", "(0;1)", prefix, ref counter);
            yield return CreateSetTest("(0;1],(1;2)", "(0;1]", prefix, ref counter);
            yield return CreateSetTest("(0;1],[1;2)", "(0;1)", prefix, ref counter);
            yield return CreateSetTest("[1;2),(0;1]", "(1;2)", prefix, ref counter);
            yield return CreateSetTest("(0;10],[5;20)", "(0;5)", prefix, ref counter);
            yield return CreateSetTest("(0;10),[5;20)", "(0;5)", prefix, ref counter);
            yield return CreateSetTest("(0;10],(5;20)", "(0;5]", prefix, ref counter);
            yield return CreateSetTest("(0;10),(5;20)", "(0;5]", prefix, ref counter);
            yield return CreateSetTest("(0;10),[5;5]", "(0;5),(5;10)", prefix, ref counter);
            yield return CreateSetTest("(0;10),(5;5)", "(0;10)", prefix, ref counter).SetDescription("Except gouged point - nothing changes");

            prefix = "sexcept";
            yield return CreateSetTest("(0;1),(1;2)", "(0;1),(1;2)", prefix, ref counter);
            yield return CreateSetTest("(0;1],(1;2)", "(0;2)", prefix, ref counter);
            yield return CreateSetTest("(0;1],[1;2)", "(0;1),(1;2)", prefix, ref counter);
            yield return CreateSetTest("(0;10],[5;20)", "(0;5),(10;20)", prefix, ref counter);
            yield return CreateSetTest("(0;10),[5;20)", "(0;5),[10;20)", prefix, ref counter);
            yield return CreateSetTest("(0;10],(5;20)", "(0;5],(10;20)", prefix, ref counter);
            yield return CreateSetTest("(0;10),(5;20)", "(0;5],[10;20)", prefix, ref counter);
            yield return CreateSetTest("(0;10),[5;5]", "(0;5),(5;10)", prefix, ref counter);
            yield return CreateSetTest("(0;10),(5;5)", "(0;10)", prefix, ref counter).SetDescription("Except gouged point - nothing changes");
        }

        [Test]
        [TestCaseSource(nameof(GetSetTests))]
        public void TestSet(List<Interval<int>> input, List<Interval<int>> output, string prefix)
        {
            if (prefix == "union")
            {
                var union = input.Union();
                AssertSets(output, union);
            }
            else if(prefix == "intersect")
            {
                var intersection = input.Intersection();
                AssertSets(output, intersection);
            }
            else if(prefix == "except")
            {
                var except = input[0].Difference(input[1]);
                AssertSets(output, except);
            }
            else if (prefix == "sexcept")
            {
                var except = input[0].SymmetricDifference(input[1]);
                AssertSets(output, except);
            }
            else
            {
                throw new NotSupportedException(prefix);
            }
        }

        private void AssertSets<T>(IEnumerable<Interval<T>> expected, IEnumerable<Interval<T>> actual)
        {
            Assert.AreEqual(string.Join(",", expected), string.Join(",", actual));
        }
        

        private static Interval<int> ParseInterval(string input)
        {
            var match = Regex.Match(input,
                @"^\s*(?<gougeL>[\(\[])\s*(?<L>[\+\-]?(\d+|inf))\s*;\s*(?<R>[\+\-]?(\d+|inf))\s*(?<gougeR>[\)\]])\s*$");
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

        private static TestCaseData CreateSetTest(string input, string result, string prefix, ref int counter)
        {
            return new TestCaseData(ParseIntervalSet(input), ParseIntervalSet(result), prefix).SetName(
                string.Format("[{0:D3}][{1}] {2} -> {3}", counter++, prefix, input, result));
        }

        private static IntervalPoint<int> ParsePoint(string input, bool gouge)
        {
            if(input == "+inf")
                return IntervalPoint<int>.PositiveInfinity;
            if(input == "-inf")
                return IntervalPoint<int>.NegativeInfinity;
            return new IntervalPoint<int>(int.Parse(input), gouge);
        }

        private static readonly IComparer<IntervalPoint<int>> Comparer =
            new IntervalPointComparer<int>(Comparer<int>.Default);

        private static readonly IComparer<IntervalPoint<int>> GougedComparer =
            new IntervalGougedPointComparer<int>(Comparer, false);
    }
}
