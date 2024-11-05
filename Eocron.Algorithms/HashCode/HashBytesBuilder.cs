using System;
using System.IO;
using Eocron.Algorithms.HashCode.Algorithms;

namespace Eocron.Algorithms.HashCode;

public sealed class HashBytesBuilder
{
    internal void WriteBytes(ReadOnlySpan<byte> data)
    {
        if (Separator != null && _stream.Length > 0)
        {
            _stream.Write(Separator);
        }

        _stream.Write(data);
    }

    public HashBytes Build()
    {
        return HashAlgorithmFactory.Compute(_stream);
    }
    
    private readonly MemoryStream _stream = new MemoryStream();
    public IHashAlgorithmFactory HashAlgorithmFactory { get; set; }
    public byte[] Separator { get; set; }
}