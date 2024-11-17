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
            _baseFolder = Path.GetFullPath(Path.Combine(Path.GetTempPath(), nameof(FileSystemTests)));
            if (Directory.Exists(_baseFolder))
            {
                Directory.Delete(_baseFolder, true);
            }

            _fs = new FileSystem(_baseFolder);
            _rnd = new Random(42);
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        }

        [TearDown]
        public void Teardown()
        {
            _fs?.Dispose();
            _cts?.Dispose();
        }
        
        [Test]
        public async Task CreateFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = _rnd.Next().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent, ct: Ct);

            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            ValidateSchema("create_file.json");
        }
        
        [Test]
        public async Task DeleteFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = _rnd.Next().ToString();

            await _fs.WriteAllTextAsync("test2.txt", "foobar", ct: Ct);
            await _fs.WriteAllTextAsync(virtualPath, expectedContent, ct: Ct);
            (await _fs.TryDeleteFileAsync(virtualPath, Ct)).Should().BeTrue();
            await ValidateFileNotExists(physicalPath, virtualPath);
            (await _fs.TryDeleteFileAsync(virtualPath, Ct)).Should().BeFalse();
            await ValidateFileNotExists(physicalPath, virtualPath);
            
            ValidateSchema("delete_file.json");
        }
        
        [Test]
        public async Task UpdateFile()
        {
            var virtualPath = "test.txt";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            var expectedContent = _rnd.Next().ToString();
            
            await _fs.WriteAllTextAsync(virtualPath, expectedContent, ct: Ct);
            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            
            expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync(virtualPath, expectedContent, ct: Ct);
            await ValidateFileExists(physicalPath, virtualPath, expectedContent);
            
            ValidateSchema("update_file.json");
        }
        
        [Test]
        public async Task CopyFile()
        {
            var expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync("test1.txt", expectedContent, ct: Ct);
            await _fs.CopyFileAsync("test1.txt", "test2.txt", Ct);
            ValidateSchema("copy_file.json");
        }
        
        [Test]
        public async Task MoveFile()
        {
            var expectedContent = _rnd.Next().ToString();
            await _fs.WriteAllTextAsync("test1.txt", expectedContent, ct: Ct);
            await _fs.MoveFileAsync("test1.txt", "test2.txt", Ct);
            ValidateSchema("move_file.json");
        }
        
        [Test]
        public async Task CreateDirectory()
        {
            var virtualPath = "test";
            var physicalPath = Path.Combine(_baseFolder, virtualPath);
            
            (await _fs.TryCreateDirectoryAsync(virtualPath, Ct)).Should().BeTrue();
            await ValidateDirectoryExists([physicalPath], [virtualPath]);
            (await _fs.TryCreateDirectoryAsync(virtualPath, Ct)).Should().BeFalse();
            await ValidateDirectoryExists([physicalPath], [virtualPath]);
            
            ValidateSchema("create_directory.json");
        }
        
        [Test]
        public async Task DeleteDirectory()
        {
            (await _fs.TryCreateDirectoryAsync("test1", Ct)).Should().BeTrue();
            (await _fs.TryCreateDirectoryAsync("test2", Ct)).Should().BeTrue();
            (await _fs.TryDeleteDirectoryAsync("test1", Ct)).Should().BeTrue();
            ValidateSchema("delete_directory.json");
        }
        
        [Test]
        public async Task MoveDirectory()
        {
            (await _fs.TryCreateDirectoryAsync("test1", Ct)).Should().BeTrue();
            await _fs.MoveDirectoryAsync("test1","test2", Ct);
            ValidateSchema("move_directory.json");
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
                Directory.Exists(pp).Should().BeTrue(because: pp);
            }

            foreach (var vp in virtualSubPaths)
            {
                (await _fs.IsDirectoryExistAsync(vp, Ct)).Should().BeTrue(because: vp);
            }
        }
        
        private async Task ValidateDirectoryNotExists(string[] physicalPaths, string[] virtualSubPaths)
        {
            foreach (var pp in physicalPaths)
            {
                Directory.Exists(pp).Should().BeFalse(because: pp);
            }

            foreach (var vp in virtualSubPaths)
            {
                (await _fs.IsDirectoryExistAsync(vp, Ct)).Should().BeFalse(because: vp);
                var a = () => Task.FromResult(_fs.GetDirectoriesAsync(vp, "*", SearchOption.AllDirectories, Ct));
                await a.Should().ThrowAsync<FileNotFoundException>();
                
                a = () => Task.FromResult(_fs.GetFilesAsync(vp, "*", SearchOption.AllDirectories, Ct));
                await a.Should().ThrowAsync<FileNotFoundException>();
            }
        }

        private async Task ValidateFileExists(string physicalPath, string virtualPath, string content)
        {
            File.Exists(physicalPath).Should().BeTrue(because: physicalPath);
            (await File.ReadAllTextAsync(physicalPath, Ct)).Should().Be(content, because: physicalPath);
            
            (await _fs.IsFileExistAsync(virtualPath, Ct)).Should().BeTrue(because: physicalPath);
            (await _fs.ReadAllTextAsync(virtualPath, ct: Ct)).Should().Be(content, because: physicalPath);
        }

        private async Task ValidateFileNotExists(string physicalPath, string virtualPath)
        {
            File.Exists(physicalPath).Should().BeFalse(because: physicalPath);
            (await _fs.IsFileExistAsync(virtualPath, Ct)).Should().BeFalse(because: physicalPath);
            var a = async () => await _fs.ReadAllTextAsync(virtualPath, ct: Ct);
            await a.Should().ThrowAsync<FileNotFoundException>();
        }
        
        private string _baseFolder;
        private FileSystem _fs;
        private Random _rnd;
        private CancellationTokenSource _cts;
        private CancellationToken Ct => _cts.Token;
    }
}