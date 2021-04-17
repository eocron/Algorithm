using Eocron.Algorithms.Levenstain;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class LevenstainTest
    {
        [Test]
        [TestCase("", "", "")]
        [TestCase("monkey", "money", "*m *o *n -k *e *y")]
        [TestCase("monkey", "monpey", "*m *o *n k->p *e *y")]
        public void Test(string source, string target, string expected)
        {
            var src = source.ToCharArray();
            var tgt = target.ToCharArray();
            var algo = LevenstainAlgorithm<char, char>.Create();

            var actual = string.Join(" ",
                algo.CalculateEdit(
                    src,
                    tgt,
                    (o, n) =>
                    {
                        if (o != 0 && n != 0)
                        {
                            if (o != n)
                                return o + "->" + n;
                            return "*" + n;
                        }
                        if (o != 0)
                            return "-" + o;
                        if (n != 0)
                            return "+" + n;
                        return "??";
                    }));

            Assert.AreEqual(expected, actual);
        }
    }
}
