using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Eocron.Algorithms.Sorted
{
    public abstract class TempFileEnumerableStorageBase<T> : IEnumerableStorage<T>, IDisposable
    {
        private readonly bool _useCompress;
        private readonly string _tempFolder;
        private readonly ConcurrentBag<string> _files = new ConcurrentBag<string>();

        public string TempFolder => _tempFolder;

        public TempFileEnumerableStorageBase(string tempFolder, bool useCompress)
        {
            _useCompress = useCompress;
            _tempFolder = tempFolder ?? Path.Combine(Path.GetTempPath(), "merge_sort");
        }

        public void Add(IEnumerable<T> data)
        {
            var filePath = GetTempFilePath();
            Stream tmp = File.OpenWrite(filePath);
            if (_useCompress)
            {
                tmp = new DeflateStream(tmp, CompressionMode.Compress, leaveOpen: false);
            }
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

        protected abstract void SerializeToStream(IEnumerable<T> data, Stream outputStream);

        protected abstract IEnumerable<T> DeserializeFromStream(Stream inputStream);

        private string GetTempFilePath()
        {
            if (!Directory.Exists(_tempFolder))
                Directory.CreateDirectory(_tempFolder);

            string filePath;
            do
            {
                filePath = Path.Combine(_tempFolder, Guid.NewGuid() + ".bin");
            } while (File.Exists(filePath));

            return filePath;
        }

        public IEnumerable<T> Take()
        {
            if (_files.TryTake(out var path))
            {
                return EnumeratePopped(path);
            }
            throw new InvalidOperationException("Storage is empty.");
        }

        private IEnumerable<T> EnumeratePopped(string path)
        {
            try
            {
                Stream tmp = File.OpenRead(path);
                if (_useCompress)
                {
                    tmp = new DeflateStream(tmp, CompressionMode.Decompress, false);
                }
                try
                {
                    //making enumerable lazy
                    foreach (var e in DeserializeFromStream(tmp))
                    {
                        yield return e;
                    }
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