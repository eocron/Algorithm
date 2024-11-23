using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Eocron.IO.Tests
{
    public static class TestResourceHelper
    {
        public static string ReadAllText(string resouceName, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            using var s = OpenRead(resouceName);
            using var sr = new StreamReader(s, encoding);
            return sr.ReadToEnd();
        }
        public static Stream OpenRead(string resourceName)
        {
            resourceName = resourceName.Replace(Path.AltDirectorySeparatorChar, '.')
                .Replace(Path.DirectorySeparatorChar, '.');

            var assembly = typeof(TestResourceHelper).Assembly;
            var found = assembly.GetManifestResourceNames()
                .Single(x => x.Contains(resourceName, StringComparison.OrdinalIgnoreCase));

            return assembly.GetManifestResourceStream(found);
        }
    }
}