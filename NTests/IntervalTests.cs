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
            yield return CreateSetTest("~(0;1)", "(-inf;0],[1;+inf)", ref counter);
            yield return CreateSetTest("~[0;1]", "(-inf;0),(1;+inf)", ref counter);
            yield return CreateSetTest("~(-inf;1)", "[1;+inf)", ref counter);
            yield return CreateSetTest("~[1;+inf)", "(-inf;1)", ref counter);
            yield return CreateSetTest("(0;1)|(1;2)", "(0;1),(1;2)", ref counter);
            yield return CreateSetTest("(0;1]|(1;2)", "(0;2)", ref counter);
            yield return CreateSetTest("(0;1]|[1;2)", "(0;2)", ref counter);
            yield return CreateSetTest("(0;1]|[1;1]", "(0;1]", ref counter);
            yield return CreateSetTest("(0;1)|[1;1]", "(0;1]", ref counter);
            yield return CreateSetTest("[1;2)|(0;1]", "(0;2)", ref counter);
            yield return CreateSetTest("(0;10]|[5;20)", "(0;20)", ref counter);

            yield return CreateSetTest("(0;1)&(1;2)", "", ref counter);
            yield return CreateSetTest("(0;1]&(1;2)", "", ref counter);
            yield return CreateSetTest("(0;1]&[1;2)", "[1;1]", ref counter);
            yield return CreateSetTest("(1;2)&[1;2)", "(1;2)", ref counter);
            yield return CreateSetTest("[1;2)&(1;2)", "(1;2)", ref counter);
            yield return CreateSetTest("(0;10]&[5;20)", "[5;10]", ref counter);
            yield return CreateSetTest("(0;10)&[5;20)", "[5;10)", ref counter);
            yield return CreateSetTest("(0;10]&(5;20)", "(5;10]", ref counter);
            yield return CreateSetTest("(0;10)&(5;20)", "(5;10)", ref counter);
            
            yield return CreateSetTest("(0;1)\\(1;2)", "(0;1)", ref counter);
            yield return CreateSetTest("(0;1]\\(1;2)", "(0;1]", ref counter);
            yield return CreateSetTest("(0;1]\\[1;2)", "(0;1)", ref counter);
            yield return CreateSetTest("[1;2)\\(0;1]", "(1;2)", ref counter);
            yield return CreateSetTest("(0;10]\\[5;20)", "(0;5)", ref counter);
            yield return CreateSetTest("(0;10)\\[5;20)", "(0;5)", ref counter);
            yield return CreateSetTest("(0;10]\\(5;20)", "(0;5]", ref counter);
            yield return CreateSetTest("(0;10)\\(5;20)", "(0;5]", ref counter);
            yield return CreateSetTest("(0;10)\\[5;5]", "(0;5),(5;10)", ref counter);
            yield return CreateSetTest("(0;10)\\(5;5)", "(0;10)", ref counter);
            
            yield return CreateSetTest("(0;1)^(1;2)", "(0;1),(1;2)", ref counter);
            yield return CreateSetTest("(0;1]^(1;2)", "(0;2)", ref counter);
            yield return CreateSetTest("(0;1]^[1;2)", "(0;1),(1;2)", ref counter);
            yield return CreateSetTest("[1;2)^(0;1]", "(0;1),(1;2)", ref counter);
            yield return CreateSetTest("(0;10]^[5;20)", "(0;5),(10;20)", ref counter);
            yield return CreateSetTest("(0;10)^[5;20)", "(0;5),[10;20)", ref counter);
            yield return CreateSetTest("(0;10]^(5;20)", "(0;5],(10;20)", ref counter);
            yield return CreateSetTest("(0;10)^(5;20)", "(0;5],[10;20)", ref counter);
            yield return CreateSetTest("(0;10)^[5;5]", "(0;5),(5;10)", ref counter);
            yield return CreateSetTest("(0;10)^(5;5)", "(0;10)", ref counter);
            yield return CreateSetTest("(0;10)^(1;2)", "(0;1],[2;10)", ref counter);
        }

        [Test]
        [TestCaseSource(nameof(GetSetTests))]
        public void TestSet(List<Interval<int>> input, List<Interval<int>> output, char op)
        {
            IEnumerable<Interval<int>> actual;
            switch (op)
            {
                case '^': actual = input[0].SymmetricDifference(input[1]); break;
                case '~': actual = input[0].Complement(); break;
                case '\\': actual = input[0].Difference(input[1]); break;
                case '|': actual = input.Union(); break;
                case '&': actual = input.Intersection(); break;
                default: throw new NotSupportedException(op.ToString());
            }
            AssertSets(output, actual);
        }

        private void AssertSets<T>(IEnumerable<Interval<T>> expected, IEnumerable<Interval<T>> actual)
        {
            Assert.AreEqual(string.Join(",", expected), string.Join(",", actual));
        }

        private static TestCaseData CreateSetTest(string input, string result, ref int counter)
        {
            var expr = ParseIntervalSet(input);
            return new TestCaseData(expr.Item1, ParseIntervalSet(result).Item1, expr.Item2).SetName(
                string.Format("[{0:D3}] {1} -> {2}", counter++, input, result));
        }

        private static Interval<int> ParseInterval(string input)
        {
            var match = Regex.Match(input,
                @"^\s*(?<gougeL>[\(\[])\s*(?<L>[\+\-]?(\d+|inf))\s*;\s*(?<R>[\+\-]?(\d+|inf))\s*(?<gougeR>[\)\]])\s*$");
            if (!match.Success)
                throw new ArgumentException("Invalid input.");

            return Interval<int>.Create(
                ParsePoint(match.Groups["L"].Value, match.Groups["gougeL"].Value == "("),
                ParsePoint(match.Groups["R"].Value, match.Groups["gougeR"].Value == ")"));
        }

        private static Tuple<List<Interval<int>>, char> ParseIntervalSet(string input)
        {
            var chars = new char[] {',', '&', '|', '^', '~', '\\'};
            var op = chars.FirstOrDefault(input.Contains);
            return Tuple.Create(
                input.Split(chars, StringSplitOptions.RemoveEmptyEntries).Select(ParseInterval).ToList(), op);
        }

        private static IntervalPoint<int> ParsePoint(string input, bool gouge)
        {
            if(input == "+inf")
                return IntervalPoint<int>.PositiveInfinity;
            if(input == "-inf")
                return IntervalPoint<int>.NegativeInfinity;
            return new IntervalPoint<int>(int.Parse(input), gouge);
        }
    }
}
