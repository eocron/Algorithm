using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace Eocron.Algorithms.Sorted
{
    public sealed class JsonMemoryCheapEnumerableStorage<T> : IEnumerableStorage<T>, IDisposable
    {
        private readonly string _tempFolder;
        private readonly JsonSerializer _serializer;
        private readonly ConcurrentBag<string> _files = new ConcurrentBag<string>();

        public string TempFolder => _tempFolder;

        public JsonMemoryCheapEnumerableStorage(string tempFolder = null)
        {
            _tempFolder = tempFolder ?? Path.Combine(Path.GetTempPath(), "merge_sort");
            _serializer = new JsonSerializer() { Formatting = Formatting.None };
        }

        private struct ObjectHolder
        {
            public T Value;
        }

        public void Push(IEnumerable<T> data)
        {
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            var filePath = Path.Combine(_tempFolder, Guid.NewGuid().ToString() + ".bin");
            using (var stream = File.OpenWrite(filePath)) 
            using (var compressed = new DeflateStream(stream, CompressionMode.Compress))
            using (var writer = new StreamWriter(compressed, Encoding.UTF8))
            using (var jwriter = new JsonTextWriter(writer))
            {
                foreach (var d in data)
                {
                    _serializer.Serialize(jwriter, new ObjectHolder() { Value = d });
                }
            }
            _files.Add(filePath);
        }

        public IEnumerable<T> Pop()
        {
            if (_files.TryTake(out var path))
            {
                return EnumeratePoped(path);
            }
            throw new InvalidOperationException("Storage is empty.");
        }

        private IEnumerable<T> EnumeratePoped(string path)
        {
            try
            {
                using var stream = File.OpenRead(path);
                using var decompressed = new DeflateStream(stream, CompressionMode.Decompress);
                using var reader = new StreamReader(decompressed, Encoding.UTF8);
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
            finally
            {
                File.Delete(path);
            }
        }

        public int Count => _files.Count;
        public void Clear()
        {
            while (_files.TryTake(out var tmp))
            {
                File.Delete(tmp);
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}