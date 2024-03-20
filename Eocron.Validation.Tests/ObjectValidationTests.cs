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
            Validate.IfObject(() => new object()).Null().Should().BeEquivalentTo(new[]
            {
                new ValidationResult { Message = "Object is not null", Type = ValidationResultType.Error }
            });
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
            Validate.IfObject(() => (object)null).NotNull().Should().BeEquivalentTo(new[]
            {
                new ValidationResult { Message = "Object is null", Type = ValidationResultType.Error }
            });
        }
    }
}