using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Validation.Tests
{
    [TestFixture]
    public class ObjectValidationTests
    {
        [Test]
        public void Null_Fail()
        {
            Validate.IfObject(() => new object()).Null().Should().BeEquivalentTo(TestHelper.VRs("Expected null, but got 'System.Object'"));
        }
        
        [Test]
        public void Null_Success()
        {
            Validate.IfObject(() => (object)null).Null().Should().BeEmpty();
        }
        
        [Test]
        public void NotNull_Fail()
        {
            Validate.IfObject(() => new object()).NotNull().Should().BeEmpty();
        }
        
        [Test]
        public void NotNull_Success()
        {
            Validate.IfObject(() => (object)null).NotNull().Should().BeEquivalentTo(TestHelper.VRs("Expected not null, but got null"));
        }
    }
}