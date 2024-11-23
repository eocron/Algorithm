using System.IO;

namespace Eocron.IO.Caching
{
    internal sealed class FileEntry
    {
        public FileEntry(FileEntryState state, string hash, string fileName, string hardLinkHash = null)
        {
            Hash = hash;
            _state = state;
            _fileName = fileName;
            _hardLinkHash = hardLinkHash;
        }

        public FileEntry CreateActiveHardLink()
        {
            return new FileEntry(FileEntryState.Active, Hash, _fileName, FileCacheShortNameHelper.GetRandom());
        }

        public FileEntry CreateTemporal()
        {
            return new FileEntry(FileEntryState.Temporal, Hash, _fileName);
        }

        public string GetDirectoryPath()
        {
            if (_hardLinkHash != null) return Path.Combine(_state.ToString(), Hash, _hardLinkHash);
            return Path.Combine(_state.ToString(), Hash);
        }

        public string GetFilePath()
        {
            if (_hardLinkHash != null) return Path.Combine(_state.ToString(), Hash, _hardLinkHash, _fileName);
            return Path.Combine(_state.ToString(), Hash, _fileName);
        }

        public string Hash { get; }
        private readonly FileEntryState _state;
        private readonly string _fileName;
        private readonly string _hardLinkHash;
    }
}