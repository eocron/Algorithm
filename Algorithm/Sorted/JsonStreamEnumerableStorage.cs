using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Eocron.Algorithms.Sorted
{
    public sealed class JsonStreamEnumerableStorage<T> : TempFileEnumerableStorageBase<T>
    {
        private readonly int _bufferSize;
        private readonly JsonSerializer _serializer;
        public JsonStreamEnumerableStorage(string tempFolder = null, int bufferSize = 64*1024, bool useCompress = false) : base(tempFolder, useCompress)
        {
            _bufferSize = bufferSize;
            _serializer = new JsonSerializer() { Formatting = Formatting.None };
        }

        private struct ObjectHolder
        {
            public T Value;
        }

        protected override void SerializeToStream(IEnumerable<T> data, Stream outputStream)
        {
            using (var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: _bufferSize))
            using (var jwriter = new JsonTextWriter(writer))
            {
                foreach (var d in data)
                {
                    _serializer.Serialize(jwriter, new ObjectHolder() { Value = d });
                }
            }
        }

        protected override IEnumerable<T> DeserializeFromStream(Stream inputStream)
        {
            using var reader = new StreamReader(inputStream, Encoding.UTF8, bufferSize: _bufferSize,
                detectEncodingFromByteOrderMarks: false);
            using var jreader = new JsonTextReader(reader);
            jreader.SupportMultipleContent = true;

            while (jreader.Read())
            {
                if (jreader.TokenType == JsonToken.StartObject)
                {
                    yield return _serializer.Deserialize<ObjectHolder>(jreader).Value;
                }
            }
        }
    }
}