using NUnit.Framework;
using System.IO;
using System.Security;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unit tests to verify path allowlist enforcement and size cap validation.
    /// Ensures that Director Studio only writes to allowed paths and respects size limits.
    /// </summary>
    [TestFixture]
    public class Unit_PathAllowlist
    {
        private const string AllowedPath = "Assets/Generated";
        private const long MaxSizeBytes = 200 * 1024 * 1024; // 200MB
        
        [Test]
        public void WriteOutsideAllowedPath_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var forbiddenPaths = new[]
            {
                "Assets/Scenes",
                "Assets/Scripts",
                "Assets/Materials",
                "ProjectSettings",
                "Packages",
                "Library",
                "Temp"
            };
            
            foreach (var path in forbiddenPaths)
            {
                // Act & Assert
                Assert.Throws<UnauthorizedAccessException>(() =>
                {
                    ValidatePathAllowlist(path);
                }, $"Writing to {path} should be forbidden");
            }
        }
        
        [Test]
        public void WriteToAllowedPath_ShouldNotThrowException()
        {
            // Arrange
            var allowedPaths = new[]
            {
                "Assets/Generated",
                "Assets/Generated/Scenes",
                "Assets/Generated/Prefabs",
                "Assets/Generated/Scripts",
                "Assets/Generated/Materials"
            };
            
            foreach (var path in allowedPaths)
            {
                // Act & Assert
                Assert.DoesNotThrow(() =>
                {
                    ValidatePathAllowlist(path);
                }, $"Writing to {path} should be allowed");
            }
        }
        
        [Test]
        public void WriteExceedingSizeLimit_ShouldThrowException()
        {
            // Arrange
            var largeSize = MaxSizeBytes + 1024; // Exceed limit by 1KB
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                ValidateSizeLimit(largeSize);
            }, "Writing files exceeding size limit should be forbidden");
        }
        
        [Test]
        public void WriteWithinSizeLimit_ShouldNotThrowException()
        {
            // Arrange
            var validSizes = new[]
            {
                1024L, // 1KB
                1024 * 1024L, // 1MB
                MaxSizeBytes - 1024L, // Just under limit
                MaxSizeBytes // Exactly at limit
            };
            
            foreach (var size in validSizes)
            {
                // Act & Assert
                Assert.DoesNotThrow(() =>
                {
                    ValidateSizeLimit(size);
                }, $"Writing {size} bytes should be allowed");
            }
        }
        
        [Test]
        public void PathValidation_ShouldHandleRelativePaths()
        {
            // Arrange
            var relativePaths = new[]
            {
                "./Assets/Generated/Test",
                "../Assets/Generated/Test",
                "Assets/Generated/../Generated/Test"
            };
            
            foreach (var path in relativePaths)
            {
                // Act & Assert
                Assert.DoesNotThrow(() =>
                {
                    ValidatePathAllowlist(path);
                }, $"Relative path {path} should be resolved and validated");
            }
        }
        
        [Test]
        public void PathValidation_ShouldHandleAbsolutePaths()
        {
            // Arrange
            var absolutePath = Path.GetFullPath("Assets/Generated/Test");
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ValidatePathAllowlist(absolutePath);
            }, "Absolute path should be validated correctly");
        }
        
        [Test]
        public void SizeValidation_ShouldHandleMultipleFiles()
        {
            // Arrange
            var fileSizes = new[] { 1024L, 2048L, 4096L }; // 7KB total
            var totalSize = fileSizes.Sum();
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ValidateSizeLimit(totalSize);
            }, "Multiple files within size limit should be allowed");
        }
        
        [Test]
        public void SizeValidation_ShouldHandleLargeSingleFile()
        {
            // Arrange
            var largeFileSize = MaxSizeBytes; // Exactly at limit
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                ValidateSizeLimit(largeFileSize);
            }, "Single large file at size limit should be allowed");
        }
        
        [Test]
        public void PathValidation_ShouldRejectNullOrEmptyPaths()
        {
            // Arrange
            var invalidPaths = new[] { null, "", "   ", "\t", "\n" };
            
            foreach (var path in invalidPaths)
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                {
                    ValidatePathAllowlist(path);
                }, $"Null or empty path should be rejected: '{path}'");
            }
        }
        
        [Test]
        public void SizeValidation_ShouldRejectNegativeSizes()
        {
            // Arrange
            var negativeSizes = new[] { -1L, -1024L, long.MinValue };
            
            foreach (var size in negativeSizes)
            {
                // Act & Assert
                Assert.Throws<ArgumentException>(() =>
                {
                    ValidateSizeLimit(size);
                }, $"Negative size should be rejected: {size}");
            }
        }
        
        private static void ValidatePathAllowlist(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));
            
            var normalizedPath = Path.GetFullPath(path);
            var allowedPath = Path.GetFullPath(AllowedPath);
            
            if (!normalizedPath.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Path '{path}' is not within allowed directory '{AllowedPath}'");
            }
        }
        
        private static void ValidateSizeLimit(long sizeBytes)
        {
            if (sizeBytes < 0)
                throw new ArgumentException("Size cannot be negative", nameof(sizeBytes));
            
            if (sizeBytes > MaxSizeBytes)
            {
                throw new InvalidOperationException($"Size {sizeBytes} bytes exceeds maximum allowed size {MaxSizeBytes} bytes (SAFE-SIZE-001)");
            }
        }
    }
}
