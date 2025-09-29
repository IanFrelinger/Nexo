using NUnit.Framework;
using System.IO;
using NexoDirectorStudio.Policies;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Integration tests to verify staging→promote atomicity and file system safety.
    /// </summary>
    [TestFixture]
    public class Integration_StagingPromote
    {
        private const string TestContent = "Test content for staging";
        private const string TestBinaryContent = "Binary test content";
        
        [SetUp]
        public void SetUp()
        {
            // Clean up any existing test files
            CleanupTestFiles();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up test files after each test
            CleanupTestFiles();
        }
        
        [Test]
        public void StageAndPromote_ShouldMoveFilesToGeneratedDirectory()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var relativePath = "Test/TestFile.txt";
            var content = System.Text.Encoding.UTF8.GetBytes(TestContent);
            
            // Act
            transaction.StageFile(relativePath, content);
            transaction.Promote();
            
            // Assert
            var targetPath = Path.Combine("Assets/Generated", relativePath);
            Assert.IsTrue(File.Exists(targetPath), "File should exist in generated directory");
            
            var actualContent = File.ReadAllText(targetPath);
            Assert.AreEqual(TestContent, actualContent, "File content should match");
        }
        
        [Test]
        public void StageMultipleFiles_ShouldPromoteAllFiles()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var files = new[]
            {
                ("Test/File1.txt", "Content 1"),
                ("Test/SubDir/File2.txt", "Content 2"),
                ("Test/File3.bin", "Binary content")
            };
            
            // Act
            foreach (var (path, content) in files)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(content);
                transaction.StageFile(path, bytes);
            }
            transaction.Promote();
            
            // Assert
            foreach (var (path, expectedContent) in files)
            {
                var targetPath = Path.Combine("Assets/Generated", path);
                Assert.IsTrue(File.Exists(targetPath), $"File {path} should exist in generated directory");
                
                var actualContent = File.ReadAllText(targetPath);
                Assert.AreEqual(expectedContent, actualContent, $"File {path} content should match");
            }
        }
        
        [Test]
        public void PromoteFailure_ShouldNotLeaveFilesInGeneratedDirectory()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var relativePath = "Test/TestFile.txt";
            var content = System.Text.Encoding.UTF8.GetBytes(TestContent);
            
            // Create a file that will cause promotion to fail
            var targetPath = Path.Combine("Assets/Generated", relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllText(targetPath, "Existing content");
            
            // Make the file read-only to cause promotion failure
            File.SetAttributes(targetPath, FileAttributes.ReadOnly);
            
            try
            {
                // Act
                transaction.StageFile(relativePath, content);
                Assert.Throws<UnauthorizedAccessException>(() => transaction.Promote());
                
                // Assert - File should still contain original content
                var actualContent = File.ReadAllText(targetPath);
                Assert.AreEqual("Existing content", actualContent, "Original file should not be modified");
            }
            finally
            {
                // Clean up read-only attribute
                File.SetAttributes(targetPath, FileAttributes.Normal);
            }
        }
        
        [Test]
        public void Rollback_ShouldRemoveStagedFiles()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var relativePath = "Test/TestFile.txt";
            var content = System.Text.Encoding.UTF8.GetBytes(TestContent);
            
            // Act
            transaction.StageFile(relativePath, content);
            transaction.Rollback();
            
            // Assert
            var stagingPath = Path.Combine("Temp/NexoBuild", relativePath);
            Assert.IsFalse(File.Exists(stagingPath), "Staged file should be removed");
            Assert.AreEqual(0, transaction.StagedFiles.Count, "No files should be staged");
        }
        
        [Test]
        public void StageFileOutsideAllowedPath_ShouldThrowException()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var forbiddenPaths = new[]
            {
                "../TestFile.txt",
                "../../TestFile.txt",
                "~/TestFile.txt",
                "Assets/Scenes/TestFile.txt",
                "ProjectSettings/TestFile.txt"
            };
            
            var content = System.Text.Encoding.UTF8.GetBytes(TestContent);
            
            // Act & Assert
            foreach (var path in forbiddenPaths)
            {
                Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    transaction.StageFile(path, content);
                }, $"Staging file at {path} should be forbidden");
            }
        }
        
        [Test]
        public void GetStagedSize_ShouldReturnCorrectSize()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var content1 = System.Text.Encoding.UTF8.GetBytes("Content 1");
            var content2 = System.Text.Encoding.UTF8.GetBytes("Content 2");
            
            // Act
            transaction.StageFile("Test/File1.txt", content1);
            transaction.StageFile("Test/File2.txt", content2);
            
            // Assert
            var expectedSize = content1.Length + content2.Length;
            Assert.AreEqual(expectedSize, transaction.GetStagedSize(), "Staged size should match expected");
        }
        
        [Test]
        public void Dispose_ShouldCleanupStagedFiles()
        {
            // Arrange
            var transaction = new FileTransaction();
            var relativePath = "Test/TestFile.txt";
            var content = System.Text.Encoding.UTF8.GetBytes(TestContent);
            
            // Act
            transaction.StageFile(relativePath, content);
            transaction.Dispose();
            
            // Assert
            var stagingPath = Path.Combine("Temp/NexoBuild", relativePath);
            Assert.IsFalse(File.Exists(stagingPath), "Staged file should be cleaned up");
        }
        
        [Test]
        public void StageTextFile_ShouldHandleTextContent()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var relativePath = "Test/TextFile.txt";
            var textContent = "This is text content with special characters: éñü";
            
            // Act
            transaction.StageTextFile(relativePath, textContent);
            transaction.Promote();
            
            // Assert
            var targetPath = Path.Combine("Assets/Generated", relativePath);
            Assert.IsTrue(File.Exists(targetPath), "Text file should exist in generated directory");
            
            var actualContent = File.ReadAllText(targetPath);
            Assert.AreEqual(textContent, actualContent, "Text content should match");
        }
        
        [Test]
        public void StageBinaryFile_ShouldHandleBinaryContent()
        {
            // Arrange
            using var transaction = new FileTransaction();
            var relativePath = "Test/BinaryFile.bin";
            var binaryContent = new byte[] { 0x00, 0x01, 0x02, 0xFF, 0xFE, 0xFD };
            
            // Act
            transaction.StageFile(relativePath, binaryContent, isBinary: true);
            transaction.Promote();
            
            // Assert
            var targetPath = Path.Combine("Assets/Generated", relativePath);
            Assert.IsTrue(File.Exists(targetPath), "Binary file should exist in generated directory");
            
            var actualContent = File.ReadAllBytes(targetPath);
            Assert.AreEqual(binaryContent, actualContent, "Binary content should match");
        }
        
        private static void CleanupTestFiles()
        {
            try
            {
                // Clean up staging directory
                if (Directory.Exists("Temp/NexoBuild"))
                {
                    Directory.Delete("Temp/NexoBuild", true);
                }
                
                // Clean up generated test files
                if (Directory.Exists("Assets/Generated/Test"))
                {
                    Directory.Delete("Assets/Generated/Test", true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
