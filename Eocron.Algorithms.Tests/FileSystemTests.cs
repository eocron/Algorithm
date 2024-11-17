using System;
using System.IO;
using System.Threading.Tasks;
using Eocron.Algorithms.FileCache;
using FluentAssertions;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class FileSystemTests
    {
        private string _baseFolder;
        private FileSystem _fs;

        [SetUp]
        public void Setup()
        {
            _baseFolder = Path.Combine(Path.GetTempPath(), nameof(FileSystemTests));
            _fs = new FileSystem(_baseFolder);
        }

        [TearDown]
        public void Teardown()
        {
            _fs.Dispose();
        }
        
        [Test]
        public async Task CreateFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = Guid.NewGuid().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);

            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
        }
        
        [Test]
        public async Task DeleteFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = Guid.NewGuid().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);
            (await _fs.TryDeleteFileAsync(virtualPath)).Should().BeTrue();
            await ValidateFileNotExists(physicalPath, virtualPath);
            (await _fs.TryDeleteFileAsync(virtualPath)).Should().BeFalse();
            await ValidateFileNotExists(physicalPath, virtualPath);
        }

        private async Task ValidateFileExists(string physicalPath, string virtualPath, string content)
        {
            File.Exists(physicalPath).Should().BeTrue();
            (await File.ReadAllTextAsync(physicalPath)).Should().Be(content);
            
            (await _fs.IsFileExistAsync(virtualPath)).Should().BeTrue();
            (await _fs.ReadAllTextAsync(virtualPath)).Should().Be(content);
        }

        private async Task ValidateFileNotExists(string physicalPath, string virtualPath)
        {
            File.Exists(physicalPath).Should().BeFalse();
            (await _fs.IsFileExistAsync(virtualPath)).Should().BeFalse();
            var a = async () => await _fs.ReadAllTextAsync(virtualPath);
            a.Should().ThrowAsync<FileNotFoundException>();
        }
    }
}