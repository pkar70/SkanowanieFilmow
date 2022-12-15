using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Chomikuj.Tests
{
    [TestFixture]
    public class ChomikujClientTests
    {
        private const int CurrentDirectoriesCount = 10;

        private const int SubDirectoriesTestDirectoryIndex = 3;
        private const int SubDirectoriesCountInTestDirectory = 2;

        private const int FilesCountTestDirectoryIndex = 0;
        private const int CurrentFilesCountInTestDirectory = 4;

        private const int FileUploadTestDirectoryIndex = 4;

        private const int PasswordSecuredTestDirectoryIndex = 1;
        private const int PasswordSecuredTestDirectoryFilesCount = 1;

        private const int AdultTestDirectoryIndex = 0;

        private const int StreamFileTestDirectoryIndex = 0;
        private const int StreamFileIndex = 3;
        private const int StreamFileSize = 2165175;

        private const int LinkTestDirectoryIndex = 0;
        private const int LinkTestFileIndex = 0;

        private const int PageTestDirectoryIndex = 2;
        private const int PageTestFilesCount = 273;

        private const int AddDirectoryTestDirectoryIndex = 0;

        private const int RemoveDirectoryTestDirectoryIndex = 0;

        private const int RemoveFileTestDirectoryIndex = 0;

        private const int HoardTestDirectoryIndex = 8;
        private const string HoardName = "Piorunica";

        private ChomikujClient _client;

        [SetUp]
        public void Setup()
        {
            _client = new ChomikujClient(TestSettings.TestLogin, TestSettings.TestPassword);            
        }

        [Test]
        public void TestDirs()
        {
            var dirs = _client.HomeDirectory.GetDirectories();
            Assert.AreEqual(CurrentDirectoriesCount, dirs.Count());
        }

        [Test]
        public void TestSubDirs()
        {
            var dirs = _client.HomeDirectory.GetDirectories().ToArray()[SubDirectoriesTestDirectoryIndex].GetDirectories();
            Assert.AreEqual(SubDirectoriesCountInTestDirectory, dirs.Count());
        }

        [Test]
        public void TestFiles()
        {
            var files = _client.HomeDirectory.GetDirectories().ToArray()[FilesCountTestDirectoryIndex].GetFiles();
            Assert.AreEqual(CurrentFilesCountInTestDirectory, files.Count());
        }

        [Test]
        public void TestDirInfo()
        {
            var info = _client.HomeDirectory.GetDirectories().ToArray()[FilesCountTestDirectoryIndex].Info;
            Assert.AreEqual(4, info.AllFilesCount);
            Assert.AreEqual(5.17 * 1024, info.SizeInKb);
            Assert.AreEqual(0, info.AudioFilesCount);
            Assert.AreEqual(0, info.TextFilesCount);
            Assert.AreEqual(1, info.ImageFilesCount);
            Assert.AreEqual(0, info.VideoFilesCount);
        }

        [Test]
        public void TestHoard()
        {
            var file = _client.HomeDirectory.GetDirectories().ToArray()[HoardTestDirectoryIndex].GetFiles().First();
            Assert.AreEqual(HoardName, file.HoardedFrom);
            Assert.AreEqual(true, file.IsHoarded);
        }

        [Test]
        public void TestUploadFile()
        {
            var buffer = new byte[1024*1024];
            new Random().NextBytes(buffer);

            var testDirectory = _client.HomeDirectory.GetDirectories().ToArray()[FileUploadTestDirectoryIndex];
            var oldCount = testDirectory.GetFiles().Count();

            var file = new NewFileRequest
            {
                FileName = "fake.webm",
                ContentType = "video/webm",
                FileStream = new MemoryStream(buffer)
            };

            testDirectory.UploadFile(file);

            var newCount = testDirectory.GetFiles().Count();

            Assert.AreEqual(oldCount + 1, newCount);
        }

        [Test]
        public void TestPasswordFiles()
        {
            var directory = _client.HomeDirectory.GetDirectories().ToArray()[PasswordSecuredTestDirectoryIndex];
            Assert.IsTrue(directory.IsPasswordProtected);
            var files = directory.GetFiles();
            Assert.AreEqual(PasswordSecuredTestDirectoryFilesCount, files.Count());
        }

        [Test]
        public void TestAdultFolder()
        {
            var directories = _client.HomeDirectory.GetDirectories().ToArray();
            var directory = directories[AdultTestDirectoryIndex];
            Assert.IsTrue(directory.HasAdultContent);
        }

        [Test]
        public void TestStreamFiles()
        {
            var content = _client.HomeDirectory.GetDirectories().ToArray()[StreamFileTestDirectoryIndex].GetFiles().ToArray()[StreamFileIndex].GetStream();
            var buffer = new byte[StreamFileSize + 10];
            var read = content.Read(buffer, 0, buffer.Length);
            Assert.AreEqual(StreamFileSize, read);
        }

        [Test]
        public void GetFileLink()
        {
            var link = _client.HomeDirectory.GetDirectories().ToArray()[LinkTestDirectoryIndex].GetFiles().ToArray()[LinkTestFileIndex].GetUrlToFile();
            Assert.IsTrue(link.Contains("File.aspx"));
        }

        [Test]
        public void GetFilesWhenThereIsMoreThenOnePage()
        {
            var files = _client.HomeDirectory.GetDirectories().ToArray()[PageTestDirectoryIndex].GetFiles();
            Assert.AreEqual(PageTestFilesCount, files.Count());
        }

        [Test]
        public void AddDirWorks()
        {
            var testDirectory = _client.HomeDirectory.GetDirectories().ToArray()[AddDirectoryTestDirectoryIndex];
            var old = testDirectory.GetDirectories().Count();

            testDirectory.CreateSubDirectory(new NewFolderRequest { Name = "z" + Guid.NewGuid().ToString("N") });

            Assert.AreEqual(old + 1, testDirectory.GetDirectories().Count());
        }

        [Test]
        public void RemoveDirWorks()
        {
            var name = Guid.NewGuid().ToString("N");
            var testDirectory = _client.HomeDirectory.GetDirectories().ToArray()[RemoveDirectoryTestDirectoryIndex];
            var old = testDirectory.GetDirectories().Count();

            _client.HomeDirectory.CreateSubDirectory(new NewFolderRequest { Name = name });
            _client.HomeDirectory.DeleteSubDirectory(name);

            Assert.AreEqual(old, testDirectory.GetDirectories().Count());
        }

        [Test]
        public void RemoveFileWorks()
        {
            var buffer = new byte[1024 * 1024];
            new Random().NextBytes(buffer);

            var testDirectory = _client.HomeDirectory.GetDirectories().ToArray()[RemoveFileTestDirectoryIndex];
            var oldCount = testDirectory.GetFiles().Count();

            var file = new NewFileRequest
            {
                FileName = "fake.webm",
                ContentType = "video/webm",
                FileStream = new MemoryStream(buffer)
            };

            testDirectory.UploadFile(file);
            var uploadedFile = testDirectory.GetFiles().First(q => q.Title == file.FileName);
            testDirectory.RemoveFile(uploadedFile);

            var newCount = testDirectory.GetFiles().Count();
            Assert.AreEqual(oldCount, newCount);
        }
    }
}
