using System.Security.Cryptography;

namespace Eocron.Algorithms.HashCode.Algorithms;

public interface IHashAlgorithmFactory
{
    string Name { get; }
    int HashByteSize { get; }
    
    int HashSize { get; }
    
    HashAlgorithm GetCachedInstance();

    HashAlgorithm Create();
}