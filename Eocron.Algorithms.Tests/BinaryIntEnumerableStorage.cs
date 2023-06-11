using System.Collections.Generic;
using System.IO;
using Eocron.Algorithms.Sorted;

namespace Eocron.Algorithms.Tests
{
    public sealed class BinaryIntEnumerableStorage : TempFileEnumerableStorageBase<int>
    {
        public BinaryIntEnumerableStorage(string tempFolder = null, bool useCompress = true) : base(tempFolder, useCompress)
        {
        }

        protected override void SerializeToStream(IReadOnlyCollection<int> data, Stream outputStream)
        {
            using var bw = new BinaryWriter(outputStream);
            bw.Write(data.Count);
            foreach (var i in data)
            {
                bw.Write(i);
            }
            bw.Flush();
        }

        protected override IEnumerable<int> DeserializeFromStream(Stream inputStream)
        {
            using var br = new BinaryReader(inputStream);
            var count = br.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                yield return br.ReadInt32();
            }
        }
    }
}