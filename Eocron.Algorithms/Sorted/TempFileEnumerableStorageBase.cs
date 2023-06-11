using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Eocron.Algorithms.Sorted
{
    public abstract class TempFileEnumerableStorageBase<T> : IEnumerableStorage<T>, IDisposable
    {
        public TempFileEnumerableStorageBase(string tempFolder, bool useCompress)
        {
            _useCompress = useCompress;
            TempFolder = tempFolder ?? Path.Combine(Path.GetTempPath(), "merge_sort");
        }

        public void Add(IReadOnlyCollection<T> data)
        {
            var filePath = GetTempFilePath();
            Stream tmp = File.OpenWrite(filePath);
            if (_useCompress) tmp = new DeflateStream(tmp, CompressionMode.Compress, false);
            try
            {
                SerializeToStream(data, tmp);
            }
            finally
            {
                tmp.Dispose();
            }

            _files.Add(filePath);
        }

        public void Clear()
        {
            while (_files.TryTake(out var tmp)) File.Delete(tmp);
        }

        public void Dispose()
        {
            Clear();
        }

        public IEnumerable<T> Take()
        {
            if (_files.TryTake(out var path)) return EnumeratePopped(path);
            throw new InvalidOperationException("Storage is empty.");
        }

        protected abstract IEnumerable<T> DeserializeFromStream(Stream inputStream);

        protected abstract void SerializeToStream(IReadOnlyCollection<T> data, Stream outputStream);

        private IEnumerable<T> EnumeratePopped(string path)
        {
            try
            {
                Stream tmp = File.OpenRead(path);
                if (_useCompress) tmp = new DeflateStream(tmp, CompressionMode.Decompress, false);
                try
                {
                    //making enumerable lazy
                    foreach (var e in DeserializeFromStream(tmp)) yield return e;
                }
                finally
                {
                    tmp.Dispose();
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        private string GetTempFilePath()
        {
            if (!Directory.Exists(TempFolder))
                Directory.CreateDirectory(TempFolder);

            string filePath;
            do
            {
                filePath = Path.Combine(TempFolder, Guid.NewGuid() + ".bin");
            } while (File.Exists(filePath));

            return filePath;
        }

        public int Count => _files.Count;

        public string TempFolder { get; }

        private readonly bool _useCompress;
        private readonly ConcurrentBag<string> _files = new ConcurrentBag<string>();
    }
}