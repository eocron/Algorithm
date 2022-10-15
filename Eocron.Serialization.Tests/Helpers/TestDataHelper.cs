using System;
using System.IO;

namespace Eocron.Serialization.Tests.Helpers
{
    public static class TestDataHelper
    {
        public static string GetPath(string relativePath)
        {
            return Path.GetFullPath(relativePath);
        }

        public static string ReadAllText(string relativePath)
        {
            return File.ReadAllText(GetPath(relativePath));
        }

        public static byte[] ReadAllBytes(string relativePath)
        {
            return File.ReadAllBytes(GetPath(relativePath));
        }
    }
}