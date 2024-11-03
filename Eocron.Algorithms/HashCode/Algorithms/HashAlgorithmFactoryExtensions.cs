using System.IO;
using System.Text;

namespace Eocron.Algorithms.HashCode.Algorithms;

public static class HashAlgorithmFactoryExtensions
{
    public static HashBytesStream Wrap(this IHashAlgorithmFactory factory, Stream stream, bool leaveOpen)
    {
        return new HashBytesStream(stream, factory, leaveOpen);
    }

    public static HashBytes Compute(this IHashAlgorithmFactory factory, byte[] data) 
    {
        return new HashBytes()
        {
            Source = factory.Name, 
            Value = factory.GetCachedInstance().ComputeHash(data)
        };
    }

    public static HashBytes Compute(this IHashAlgorithmFactory factory, byte[] data, int offset, int count)
    {
        return new HashBytes()
        {
            Source = factory.Name, 
            Value = factory.GetCachedInstance().ComputeHash(data, offset, count)
        };
    }
        
    public static HashBytes Compute(this IHashAlgorithmFactory factory, string data, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        return new HashBytes()
        {
            Source = factory.Name, 
            Value = factory.GetCachedInstance().ComputeHash(encoding.GetBytes(data))
        };
    }
}