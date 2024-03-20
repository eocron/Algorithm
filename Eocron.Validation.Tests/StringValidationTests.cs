using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Validation.Tests
{
    [TestFixture]
    public class StringValidationTests
    {
        [Test]
        public void Match_Success()
        {
            Validate.IfObject("aaaaa").Match("^a+$").Should().BeEmpty();
        }
        
        [Test]
        public void Match_Fail()
        {
            Validate.IfObject("aaaaa").Match("^b+$").Should().BeEquivalentTo(TestHelper.VRs("String 'aaaaa' doesn't match regex pattern '^b+$'"));
        }
    }
}