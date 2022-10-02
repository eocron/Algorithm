namespace Eocron.Serialization.Tests.Helpers;

public static class TestDataHelper
{
    public static string GetPath(string relativePath)
    {
        var path = new Uri(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase) ?? string.Empty).LocalPath;
        return Path.Combine(path, relativePath);
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