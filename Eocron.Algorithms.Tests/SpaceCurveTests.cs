using Eocron.Algorithms.SpaceCurves;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class SpaceCurveTests
    {
        [Test]
        public void ZCurve_Check()
        {
            var input = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.Interleave(input, 4);
            actual.Should().Equal(new[] {1, 5, 9, 2, 6, 10, 3, 7, 11, 4, 8, 12});
        }
    }
}