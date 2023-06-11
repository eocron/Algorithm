// ReSharper disable InconsistentNaming

using System.Runtime.CompilerServices;

namespace Eocron.Algorithms.EqualityComparers.xxHash
{
    public static partial class xxHash64
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong XXH64_round(ulong acc, ulong input)
        {
            acc += input * XXH_PRIME64_2;
            acc = XXH_rotl64(acc, 31);
            acc *= XXH_PRIME64_1;
            return acc;
        }
    }
}