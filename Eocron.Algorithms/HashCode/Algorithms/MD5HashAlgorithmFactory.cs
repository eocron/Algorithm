using System;
using System.Security.Cryptography;

namespace Eocron.Algorithms.HashCode.Algorithms;

public sealed class MD5HashAlgorithmFactory : HashAlgorithmFactoryBase
{
    public override HashAlgorithm Create()
    {
        return MD5.Create();
    }
    
    public override HashAlgorithm GetCachedInstance()
    {
        return HashAlgorithm ??= Create();
    }

    [ThreadStatic] private static HashAlgorithm HashAlgorithm;
}