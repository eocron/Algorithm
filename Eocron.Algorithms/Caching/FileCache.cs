using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.HashCode.Algorithms;
using Eocron.Algorithms.Hex;
using Eocron.Algorithms.IO;

namespace Eocron.Algorithms.Caching
{
    [Obsolete("Still need TODO: thread synchronization, testing")]
    public sealed class FileCache : IFileCache, IDisposable, IAsyncDisposable
    {
        public FileCache(IFileSystem fs)
        {
            _fs = fs ?? throw new ArgumentNullException(nameof(fs));
            _keyHash = new SHA1HashAlgorithmFactory().Create();
        }

        public void Dispose()
        {
            _sync?.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_sync is IAsyncDisposable syncAsyncDisposable)
                await syncAsyncDisposable.DisposeAsync();
            else if (_sync != null)
                _sync.Dispose();
        }

        public async Task<Stream> GetOrAddAsync(string key,
            Func<string, CancellationToken, Task<Stream>> streamProvider, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            var pKey = GetPhysicalKey(key);
            if (_entries[FileEntryState.Active].TryGetValue(pKey, out var entry))
                return await _fs.OpenFileAsync(entry.FilePath, FileMode.Open, ct).ConfigureAwait(false);

            FileEntry tmpEntry;
            await using (var srcStream = await streamProvider(key, ct).ConfigureAwait(false))
            {
                var tmp = await OpenTemporalFileEntry(pKey, ct).ConfigureAwait(false);
                tmpEntry = tmp.Item1;
                await using (var tgtStream = tmp.Item2)
                {
                    await srcStream.CopyToAsync(tgtStream, ct).ConfigureAwait(false);
                }
            }

            await SwitchStateAsync(tmpEntry, FileEntryState.Active, ct).ConfigureAwait(false);
            return await _fs.OpenFileAsync(tmpEntry.FilePath, FileMode.Open, ct).ConfigureAwait(false);
        }

        public async Task<Stream> TryGetAsync(string key, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            var pKey = GetPhysicalKey(key);
            if (!_entries[FileEntryState.Active].TryGetValue(pKey, out var entry)) return null;

            return await _fs.OpenFileAsync(entry.FilePath, FileMode.Open, ct).ConfigureAwait(false);
        }

        public async Task<bool> TryRemoveAsync(string key, CancellationToken ct = default)
        {
            await EnsureInitializedAsync(ct).ConfigureAwait(false);
            var pKey = GetPhysicalKey(key);
            if (!_entries[FileEntryState.Active].TryGetValue(pKey, out var entry)) return false;
            await entry.SwitchStateAsync(FileEntryState.Deleted, ct).ConfigureAwait(false);
            _entries[FileEntryState.Active].TryRemove(pKey, out _);
            return true;
        }

        private async Task EnsureInitializedAsync(CancellationToken ct)
        {
            if (_initialized) return;

            await _sync.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_initialized) return;

                await InitializeAsync(ct).ConfigureAwait(false);
                _initialized = true;
            }
            finally
            {
                _sync.Release();
            }
        }

        private string GetPhysicalKey(string virtualKey)
        {
            return _keyHash.ComputeHash(Encoding.UTF8.GetBytes(virtualKey)).ToHexString(HexFormatting.None);
        }

        private async IAsyncEnumerable<FileEntry> GetStoredEntriesAsync([EnumeratorCancellation] CancellationToken ct,
            params FileEntryState[] states)
        {
            foreach (var state in states)
            {
                ct.ThrowIfCancellationRequested();
                await foreach (var batch in _fs
                                   .GetFilesAsync(state.ToString(), "*.cached", SearchOption.TopDirectoryOnly, ct)
                                   .ConfigureAwait(false))
                foreach (var path in batch)
                    yield return new FileEntry(_fs, path, state);
            }
        }


        private async Task InitializeAsync(CancellationToken ct)
        {
            foreach (var stateName in Enum.GetNames(typeof(FileEntryState)))
                await _fs.TryCreateDirectoryAsync(stateName, ct).ConfigureAwait(false);

            var tmp = new ConcurrentDictionary<FileEntryState, ConcurrentDictionary<string, FileEntry>>();
            await foreach (var entry in GetStoredEntriesAsync(ct, FileEntryState.Active, FileEntryState.Deleted,
                               FileEntryState.Temporal).ConfigureAwait(false))
            {
                if (!tmp.TryGetValue(entry.CurrentState, out var set))
                {
                    set = new ConcurrentDictionary<string, FileEntry>();
                    tmp.TryAdd(entry.CurrentState, set);
                }

                set.TryAdd(entry.Key, entry);
            }

            _entries = tmp;
        }

        private async Task<Tuple<FileEntry, Stream>> OpenTemporalFileEntry(string physicalKey, CancellationToken ct)
        {
            var entry = new FileEntry(
                _fs,
                Path.Combine(FileEntryState.Temporal.ToString(), physicalKey + ".cached"),
                FileEntryState.Temporal);

            var result = Tuple.Create(entry,
                await _fs.OpenFileAsync(entry.FilePath, FileMode.CreateNew, ct).ConfigureAwait(false));

            _entries[entry.CurrentState].TryAdd(physicalKey, entry);
            return result;
        }

        private async Task SwitchStateAsync(FileEntry entry, FileEntryState newState, CancellationToken ct)
        {
            var prevState = entry.CurrentState;
            await entry.SwitchStateAsync(newState, ct).ConfigureAwait(false);
            _entries[prevState].TryRemove(entry.Key, out _);
            _entries[newState].TryAdd(entry.Key, entry);
        }

        private readonly HashAlgorithm _keyHash;
        private readonly IFileSystem _fs;

        private readonly SemaphoreSlim _sync = new(1);
        private volatile bool _initialized;
        private ConcurrentDictionary<FileEntryState, ConcurrentDictionary<string, FileEntry>> _entries;

        private sealed class FileEntry
        {
            public FileEntry(IFileSystem fs, string filePath, FileEntryState state)
            {
                _fs = fs;
                FilePath = filePath;
                CurrentState = state;
                Key = Path.GetFileNameWithoutExtension(filePath);
            }

            public async Task SwitchStateAsync(FileEntryState newState, CancellationToken ct)
            {
                if (newState == CurrentState)
                    return;
                var newFilePath = Path.Combine(newState.ToString(), Key + ".cached");
                await _fs.MoveFileAsync(FilePath, newFilePath, ct).ConfigureAwait(false);
                FilePath = newFilePath;
                CurrentState = newState;
            }

            public FileEntryState CurrentState { get; private set; }
            public string FilePath { get; private set; }
            public string Key { get; }
            private readonly IFileSystem _fs;
        }

        private enum FileEntryState
        {
            Temporal,
            Active,
            Deleted
        }
    }
}