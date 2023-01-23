using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using File = System.IO.File;

namespace Eocron.Algorithms.Sorted
{
    public sealed class JsonEnumerableStorage<T> : IEnumerableStorage<T>, IDisposable
    {
        private readonly string _tempFolder;
        private readonly JsonSerializer _serializer;
        private readonly ConcurrentBag<string> _files = new ConcurrentBag<string>();

        public string TempFolder => _tempFolder;

        public JsonEnumerableStorage(string tempFolder = null)
        {
            _tempFolder = tempFolder ?? Path.Combine(Path.GetTempPath(), "merge_sort");
            _serializer = new JsonSerializer(){Formatting = Formatting.None};
        }

        private sealed class ObjectHolder
        {
            public T V { get; set; }
        }

        public void Push(IEnumerable<T> data)
        {
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            var filePath = Path.Combine(_tempFolder, Guid.NewGuid().ToString() + ".json");
            using var stream = File.OpenWrite(filePath);
            using var streamWriter = new StreamWriter(stream);
            using var writer = new JsonTextWriter(streamWriter);
            foreach (var d in data)
            {
                _serializer.Serialize(writer, new ObjectHolder(){V = d});
            }
            _files.Add(filePath);
        }

        public IEnumerable<T> Pop()
        {
            if (_files.TryTake(out var path))
            {
                try
                {
                    using var stream = File.OpenRead(path);
                    using var streamReader = new StreamReader(stream);
                    using var reader = new JsonTextReader(streamReader);
                    reader.SupportMultipleContent = true;

                    while (reader.Read())
                    {
                        if (reader.TokenType != JsonToken.StartObject) continue;
                        var tmp = _serializer.Deserialize<ObjectHolder>(reader);
                        yield return tmp.V;
                    }
                    yield break;
                }
                finally
                {
                    File.Delete(path);
                }
            }

            throw new InvalidOperationException("Storage is empty.");
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