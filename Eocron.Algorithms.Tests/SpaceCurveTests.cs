using Eocron.Algorithms.SpaceCurves;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class SpaceCurveTests
    {
        [Test]
        public void ZCurve_Check_Single_Base3()
        {
            var input = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.InterleaveSingle(input, 3);
            actual.Should().Equal(new[] {1, 5, 9, 2, 6, 10, 3, 7, 11, 4, 8, 12});
        }
        
        [Test]
        public void ZCurve_Check_Single_Base2()
        {
            var input = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.InterleaveSingle(input, 2);
            actual.Should().Equal(new[] {1, 7, 2, 8, 3, 9, 4, 10, 5, 11, 6, 12});
        }
        
        [Test]
        public void ZCurve_Check_Multiple_SameSize()
        {
            var input1 = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var input2 = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            var actual = ZCurve.InterleaveMultiple(input1, input2);
            actual.Should().Equal(new[] {1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12});
        }
        
        
                
        [Test]
        public void ZCurve_Check_Multiple_DifferentSize()
        {
            var input1 = new[] { 1, 2, 3, 4, 5 };
            var input2 = new[] { 6 };
            var input3 = new[] { 7 };
            var actual = ZCurve.InterleaveMultiple(input1, input2, input3);
            actual.Should().Equal(new[] {1, 2, 3, 4, 5, 6, 7});
        }
        
        [Test]
        public void ZCurve_Check_Multiple_DifferentSize_Gcd()
        {
            var input1 = new[] { 1, 2, 3, 4 };
            var input2 = new[] { 6, 7 };
            var input3 = new[] { 8, 9 };
            var actual = ZCurve.InterleaveMultiple(input1, input2, input3);
            actual.Should().Equal(new[] {1, 2, 6, 8, 3, 4, 7, 9});
            actual = ZCurve.InterleaveMultiple(input3, input2, input1);
            actual.Should().Equal(new[] {8, 6, 1, 2, 9, 7, 3, 4});
        }
    }
}