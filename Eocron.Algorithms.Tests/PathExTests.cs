using Eocron.Algorithms.Paths;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class PathExTests
    {
        [Test]
        [TestCase(@"a", @"a", Category = "simple")]
        [TestCase(@"a/a", @"a/a", Category = "simple")]
        [TestCase("a\\a", @"a/a", Category = "simple")]
        [TestCase(@"/a/b/", @"/a/b/", Category = "simple")]
        [TestCase(@"a/../b", @"b", Category = "parent")]
        [TestCase(@"a/../b/../../../c/", @"c/", Category = "parent")]
        [TestCase(@"F://a/../b/../../../c", @"F:/c", Category = "parent")]
        [TestCase(@"a/./b", @"a/b", Category = "dot")]
        [TestCase(@".", @".", Category = "dot")]
        [TestCase(@"./", @"./", Category = "dot")]
        [TestCase(@"/./", @"./", Category = "dot")]
        public void Check(string inputPath, string expectedPath)
        {
            Assert.AreEqual(expectedPath, PathEx.Eval(inputPath, '/'));
        }
    }
}