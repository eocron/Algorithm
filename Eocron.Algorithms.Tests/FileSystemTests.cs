using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Eocron.Algorithms.IO;
using Eocron.Algorithms.Tests.Core;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Eocron.Algorithms.Tests
{
    [TestFixture]
    public class FileSystemTests
    {
        [SetUp]
        public void Setup()
        {
            _baseFolder = Path.Combine(Path.GetTempPath(), nameof(FileSystemTests));
            if (Directory.Exists(_baseFolder))
            {
                Directory.Delete(_baseFolder, true);
            }

            _fs = new FileSystem(_baseFolder);
            _rnd = new Random(42);
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
            var expectedContent = _rnd.Next().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);

            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            ValidateSchema("create_file.json");
        }
        
        [Test]
        public async Task DeleteFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = _rnd.Next().ToString();

            await _fs.WriteAllTextAsync("test2.txt", "foobar");
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);
            (await _fs.TryDeleteFileAsync(virtualPath)).Should().BeTrue();
            await ValidateFileNotExists(physicalPath, virtualPath);
            (await _fs.TryDeleteFileAsync(virtualPath)).Should().BeFalse();
            await ValidateFileNotExists(physicalPath, virtualPath);
            
            ValidateSchema("delete_file.json");
        }
        
        [Test]
        public async Task UpdateFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = _rnd.Next().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);
            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            
            expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync(virtualPath, expectedContent);
            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            
            ValidateSchema("update_file.json");
        }
        
        [Test]
        public async Task CopyFile()
        {
            var expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync("test1.txt", expectedContent);
            await _fs.CopyFileAsync("test1.txt", "test2.txt");
            ValidateSchema("copy_file.json");
        }
        
        [Test]
        public async Task MoveFile()
        {
            var expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync("test1.txt", expectedContent);
            await _fs.MoveFileAsync("test1.txt", "test2.txt");
            ValidateSchema("move_file.json");
        }
        
        [Test]
        public async Task CreateDirectory()
        {
            var virtualPath = "test";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            
            (await _fs.TryCreateDirectoryAsync(virtualPath)).Should().BeTrue();
            await ValidateDirectoryExists([physicalPath], [virtualPath]);
            (await _fs.TryCreateDirectoryAsync(virtualPath)).Should().BeFalse();
            await ValidateDirectoryExists([physicalPath], [virtualPath]);
            
            ValidateSchema("create_directory.json");
        }
        
        [Test]
        public async Task DeleteDirectory()
        {
            (await _fs.TryCreateDirectoryAsync("test1")).Should().BeTrue();
            (await _fs.TryCreateDirectoryAsync("test2")).Should().BeTrue();
            (await _fs.TryDeleteDirectoryAsync("test1")).Should().BeTrue();
            ValidateSchema("delete_directory.json");
        }

        private void ValidateSchema(string expectedSchemaPath)
        {
            var schema = TestResourceHelper.ReadAllText(expectedSchemaPath).Replace("\n\r", "\n").Trim();
            var actualSchema = SerializeSchema(_baseFolder, _baseFolder).Replace("\n\r", "\n").Trim();
            try
            {
                actualSchema.Should().Be(schema);
            }
            catch (Exception)
            {
                Console.WriteLine("EXPECTED:");
                Console.WriteLine(schema);
                Console.WriteLine("ACTUAL:");
                Console.WriteLine(actualSchema);
                throw;
            }

        }
        private static string SerializeSchema(string basePath, string folderPath)
        {
            var directories = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
                .Select(x=> x.Replace(basePath, "root").Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/'));
            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories).Select(x=> new
            {
                path = x.Replace(basePath, "root").Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/'),
                content = File.ReadAllText(x)
            }).ToList();

            return JsonConvert.SerializeObject(new
            {
                dirs = directories,
                files = files
            }, Formatting.Indented);
        }
        
        private async Task ValidateDirectoryExists(string[] physicalPaths, string[] virtualSubPaths)
        {
            foreach (var pp in physicalPaths)
            {
                Directory.Exists(pp).Should().BeTrue();
            }

            foreach (var vp in virtualSubPaths)
            {
                (await _fs.IsDirectoryExistAsync(vp)).Should().BeTrue();
            }
        }
        
        private async Task ValidateDirectoryNotExists(string[] physicalPaths, string[] virtualSubPaths)
        {
            foreach (var pp in physicalPaths)
            {
                Directory.Exists(pp).Should().BeFalse();
            }

            foreach (var vp in virtualSubPaths)
            {
                (await _fs.IsDirectoryExistAsync(vp)).Should().BeFalse();
                var a = () => Task.FromResult(_fs.GetDirectoriesAsync(vp, "*", SearchOption.AllDirectories, CancellationToken.None));
                await a.Should().ThrowAsync<FileNotFoundException>();
                
                a = () => Task.FromResult(_fs.GetFilesAsync(vp, "*", SearchOption.AllDirectories, CancellationToken.None));
                await a.Should().ThrowAsync<FileNotFoundException>();
            }
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
            await a.Should().ThrowAsync<FileNotFoundException>();
        }
        
        private string _baseFolder;
        private FileSystem _fs;
        private Random _rnd;
    }
}