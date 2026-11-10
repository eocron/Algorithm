using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eocron.Algorithms.Primes;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class PrimeGapWheelTests
    {
        [Test]
        [TestCase(1, 2, new[]{1})]
        [TestCase(2, 3, new[]{2})]
        [TestCase(3, 5, new[]{4,2})]
        [TestCase(4, 7, new[]{6,4,2,4,2,4,6,2})]
        [TestCase(5, 11, new[]{10,2,4,2,4,6,2,6,4,2,4,6,6,2,6,4,2,6,4,6,8,4,2,4,2,4,8,6,4,6,2,4,6,2,6,6,4,2,4,6,2,6,4,2,4,2,10,2})]
        //[TestCase(6, 13, new[]{10,2,4,2,4,6,2,6,4,2,4,6,6,2,6,4,2,6,4,6,8,4,2,4,2,4,8,6,4,6,2,4,6,2,6,6,4,2,4,6,2,6,4,2,4,2,10,2})]
        public void CheckGaps(int targetLevel, long nextPrime, int[] expectedGaps)
        {
            var w = new PrimeGapWheel();
            for (var level = 1; level < targetLevel; level++)
            {
                w = w.CreateNextWheel();
            }



            Console.WriteLine(string.Join(",", w.Gaps()));
            Console.WriteLine(string.Join("\n", w.Gaps().Select(x=> Convert.ToString(x, 2).PadLeft(5,' '))));
            DrawGaps(w.Gaps());
            
            var actual = w.Gaps().ToArray();
            actual.Should().Equal(expectedGaps);
            w.NextPrime().Should().Be(nextPrime);
        }
        
        static void DrawGaps(IEnumerable<int> gaps, int maxColumns = 100, char bar = '█')
        {
            int[] all = gaps.ToArray();
            int[] g = all.Take(maxColumns).ToArray();
            if (g.Length == 0) return;

            int max = g.Max();
            for (int row = max; row >= 1; row--)
            {
                var sb = new StringBuilder();
                foreach (int v in g) sb.Append(v >= row ? bar + " " : "  ");
                Console.WriteLine(sb.ToString().TrimEnd());
            }
            if (all.Length > g.Length)
                Console.WriteLine($"    ... and {all.Length - g.Length} more gaps (max height {max})");
        }
    }
}