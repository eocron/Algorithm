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
            Validate.If("aaaaa").Match("^a+$").Should().BeEmpty();
        }
        
        [Test]
        public void Match_Fail()
        {
            Validate.If("aaaaa").Match("^b+$").Should().BeEquivalentTo(TestHelper.VRs("Expected 'aaaaa' to match regex '^b+$'"));
        }
    }
}