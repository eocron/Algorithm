﻿using System;
using System.Security.Cryptography;

namespace Eocron.Algorithms.HashCode.Algorithms;

public sealed class SHA256HashAlgorithmFactory : HashAlgorithmFactoryBase
{
    public override HashAlgorithm Create()
    {
        return SHA256.Create();
    }
    
    public override HashAlgorithm GetCachedInstance()
    {
        return HashAlgorithm ??= Create();
    }

    [ThreadStatic] private static HashAlgorithm HashAlgorithm;
}