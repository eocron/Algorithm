﻿using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Validation.Tests
{
    [TestFixture]
    public class BasicValidationTests
    {
        [Test]
        public void Validation_Success()
        {
            Validate.If(() => "a" == "a").WithMessage("foobar").Should().BeEmpty();
        }
        
        [Test]
        public void Validation_Fail()
        {
            Validate.If(() => "a" != "a").WithMessage("foobar").Should().BeEquivalentTo(TestHelper.VRs("foobar"));
        }
        
        [Test]
        public void Ignore_Fail()
        {
            Validate.If(() => "a" != "a").WithMessage("foobar").IgnoreOn(false).Should().BeEquivalentTo(TestHelper.VRs("foobar"));
        }
        
        [Test]
        public void Ignore_Success()
        {
            Validate.If(() => "a" != "a").WithMessage("foobar").IgnoreOn(true).Should().BeEmpty();
        }

        [Test]
        [TestCase(true, true, true, new string[0])]
        [TestCase(true, true, false, new string[0])]
        [TestCase(true, false, true, new []{"thenBranch"})]
        [TestCase(true, false, false, new []{"thenBranch"})]
        [TestCase(false, true, true, new []{"topBranch"})]
        [TestCase(false, true, false, new []{"topBranch", "elseBranch"})]
        [TestCase(false, false, true, new []{"topBranch"})]
        [TestCase(false, false, false, new []{"topBranch", "elseBranch"})]
        public void Branching(bool topBranch, bool thenBranch, bool elseBranch, string[] messages)
        {
            var should =Validate.If(() => topBranch)
                .Then(() => Validate.If(() => thenBranch).WithMessage("thenBranch"))
                .Else(() => Validate.If(() => elseBranch).WithMessage("elseBranch"))
                .WithMessage("topBranch")
                .Should();

            if (messages == null || messages.Length == 0)
            {
                should.BeEmpty();
            }
            else
            {
                should.BeEquivalentTo(TestHelper.VRs(messages));
            }
        }

        [Test]
        public void Chaining()
        {
            Validate.If(() => false)
                .WithMessage("first")
                .Or(() => false)
                .Then(() => Validate.If(() => false).WithMessage("not fired"))
                .Else(() => Validate.If(() => false).WithMessage("betweenFirstAndSecond"))
                //no message
                .Or(() => false)
                .WithMessage("second")
                .Or(() => true)
                .Then(() => Validate.If(() => false).WithMessage("betweenSecondAndThird"))
                .Else(() => Validate.If(() => false).WithMessage("not fired"))
                .WithMessage("not fired betweenSecondAndThird")
                .Or(() => false)
                .WithMessage("third")
                .Should().BeEquivalentTo(TestHelper.VRs("first", "betweenFirstAndSecond", "second", "betweenSecondAndThird", "third"));
        }
        
        [Test]
        public void ObjectValidationExample()
        {
            var obj = new TestObject()
            {
                Id = 1,
                Name = "one",
                Children = new[]
                {
                    new TestObject()
                    {
                        Id = 2,
                        Name = "two"
                    },
                    new TestObject()
                    {
                        Id = 3,
                        Name = "three"
                    }
                }
            };

            Validate.IfObject(obj)
                .NotNull()
                .WithMessage("Null test object")
                .Then(() => Validate
                    .IfObject(obj.Children)
                    .NotNull()
                    .WithMessage("children is null")
                    .Or(() => obj.Id > 0)
                    .WithMessage("id is below 1"))
                .Should().BeEmpty();
        }
        
        public class TestObject
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public TestObject[] Children { get; set; }
        }
    }
}