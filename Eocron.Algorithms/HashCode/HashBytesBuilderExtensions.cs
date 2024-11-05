using System;
using System.Text;
using Eocron.Algorithms.HashCode.Algorithms;
using Newtonsoft.Json;

namespace Eocron.Algorithms.HashCode;

public static class HashBytesBuilderExtensions
{
    public static HashBytesBuilder AddBytes(this HashBytesBuilder builder, ReadOnlySpan<byte> data)
    {
        builder.WriteBytes(data);
        return builder;
    }
    
    public static HashBytesBuilder AddString(this HashBytesBuilder builder, string str, Encoding encoding = null)
    {
        encoding ??= Encoding.UTF8;
        builder.WriteBytes(str == null ? Array.Empty<byte>() : encoding.GetBytes(str));
        return builder;
    }
    
    public static HashBytesBuilder AddObject(this HashBytesBuilder builder, object obj)
    {
        builder.WriteBytes(obj == null ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj)));
        return builder;
    }
        
    public static HashBytesBuilder WithHashAlgorithmFactory(this HashBytesBuilder builder, IHashAlgorithmFactory factory)
    {
        builder.HashAlgorithmFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        return builder;
    }
        
    public static HashBytesBuilder WithSeparator(this HashBytesBuilder builder, byte[] separator)
    {
        builder.Separator = separator ?? throw new ArgumentNullException(nameof(separator));
        return builder;
    }
}