using System;
using System.Security.Cryptography;

namespace Eocron.Algorithms.HashCode.Algorithms;

public abstract class HashAlgorithmFactoryBase : IHashAlgorithmFactory
{
    public string Name => this.GetType().Name;
    public int HashByteSize => GetCachedInstance().HashSize >> 3;
    public int HashSize => GetCachedInstance().HashSize;
    public abstract HashAlgorithm GetCachedInstance();

    public abstract HashAlgorithm Create();
}