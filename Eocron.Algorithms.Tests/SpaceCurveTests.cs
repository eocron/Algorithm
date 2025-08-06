using Eocron.Algorithms.SpaceCurves;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class SpaceCurveTests
    {
        [Test]
        public void ZCurve_Check_Base3()
        {
            var input = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.Interleave(input, 3);
            actual.Should().Equal(new[] {1, 5, 9, 2, 6, 10, 3, 7, 11, 4, 8, 12});
        }
        
        [Test]
        public void ZCurve_Check_Base2()
        {
            var input = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.Interleave(input, 2);
            actual.Should().Equal(new[] {1, 7, 2, 8, 3, 9, 4, 10, 5, 11, 6, 12});
        }
        
        [Test]
        public void ZCurve_Check_Multiple()
        {
            var input1 = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var input2 = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.Interleave(input1, input2);
            actual.Should().Equal(new[] {1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12});
        }
    }
}