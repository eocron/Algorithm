using System;

namespace Eocron.Algorithms.FileCheckSum
{
    public interface IHashAlgorithmLazyCheckSum : ILazyCheckSum<byte[]>, IDisposable
    {

    }
}
