using Algorithm.Hex;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace NTests
{
    [TestFixture]
    public class HexTests
    {
        [Test]
        [TestCase("0x01aB", HexFormatting.Unix, "0x01AB")]
        [TestCase("01aB", HexFormatting.None, "01AB")]
        [TestCase("\\x01aB", HexFormatting.Esc, "\\x01AB")]
        [TestCase("%01aB", HexFormatting.Uri, "%01AB")]
        [TestCase("&#x01aB", HexFormatting.Xml, "&#x01AB")]
        [TestCase("U+01aB", HexFormatting.Unicode, "U+01AB")]
        [TestCase("#01aB", HexFormatting.HtmlColor, "#01AB")]
        [TestCase("0x", HexFormatting.Unix, "0x")]
        [TestCase("", HexFormatting.None, "")]
        [TestCase("\\x", HexFormatting.Esc, "\\x")]
        [TestCase("%", HexFormatting.Uri, "%")]
        [TestCase("&#x", HexFormatting.Xml, "&#x")]
        [TestCase("U+", HexFormatting.Unicode, "U+")]
        [TestCase("#", HexFormatting.HtmlColor, "#")]
        public void Convert(string input, HexFormatting formatting, string expected)
        {
            var bytes = HexEncoding.Convert(input, formatting);
            var actual = HexEncoding.Convert(bytes, formatting, upperCase: true);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestCase("", HexFormatting.Unix, "Invalid hex format. (Parameter 'str')")]
        [TestCase("0x0", HexFormatting.Unix, "Invalid hex length. (Parameter 'str')")]
        [TestCase(null, HexFormatting.Unix, "Value cannot be null. (Parameter 'str')")]
        [TestCase("0xfoobar", HexFormatting.Unix, "Invalid hex character.")]
        public void ConvertError(string input, HexFormatting formatting, string expectedError)
        {
            Assert.That(() => HexEncoding.Convert(input, formatting),
               Throws.Exception
                 .InstanceOf<ArgumentException>()
                 .With.Message.EqualTo(expectedError));
        }
    }
}
