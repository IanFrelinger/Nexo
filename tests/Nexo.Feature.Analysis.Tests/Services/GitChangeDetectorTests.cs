using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;
using Xunit;

namespace Nexo.Feature.Analysis.Tests.Services
{
    /// <summary>
    /// Tests for the Git change detector service.
    /// </summary>
    public class GitChangeDetectorTests
    {
        private readonly Mock<ILogger<GitChangeDetector>> _mockLogger;
        private readonly GitChangeDetector _gitChangeDetector;

        public GitChangeDetectorTests()
        {
            _mockLogger = new Mock<ILogger<GitChangeDetector>>();
            _gitChangeDetector = new GitChangeDetector(_mockLogger.Object);
        }

        [Fact]
        public async Task IsGitRepositoryAsync_ShouldReturnFalse_WhenNotInGitRepository()
        {
            // Arrange - Create a temporary directory that's not a Git repository
            var tempDir = Path.GetTempPath();
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Act
                var result = await _gitChangeDetector.IsGitRepositoryAsync();

                // Assert
                Assert.False(result);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [Fact]
        public async Task GetUncommittedChangesAsync_ShouldReturnEmptyList_WhenNotInGitRepository()
        {
            // Arrange - Create a temporary directory that's not a Git repository
            var tempDir = Path.GetTempPath();
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Act
                var result = await _gitChangeDetector.GetUncommittedChangesAsync();

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [Fact]
        public async Task GetChangedFilesAsync_ShouldReturnEmptyList_WhenNotInGitRepository()
        {
            // Arrange - Create a temporary directory that's not a Git repository
            var tempDir = Path.GetTempPath();
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Act
                var result = await _gitChangeDetector.GetChangedFilesAsync("HEAD~1");

                // Assert
                Assert.NotNull(result);
                Assert.Empty(result);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [Fact]
        public async Task GetCurrentBranchAsync_ShouldReturnEmptyString_WhenNotInGitRepository()
        {
            // Arrange - Create a temporary directory that's not a Git repository
            var tempDir = Path.GetTempPath();
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Act
                var result = await _gitChangeDetector.GetCurrentBranchAsync();

                // Assert
                Assert.Equal(string.Empty, result);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [Fact]
        public async Task GetLatestCommitHashAsync_ShouldReturnEmptyString_WhenNotInGitRepository()
        {
            // Arrange - Create a temporary directory that's not a Git repository
            var tempDir = Path.GetTempPath();
            var originalDir = Directory.GetCurrentDirectory();
            
            try
            {
                Directory.SetCurrentDirectory(tempDir);

                // Act
                var result = await _gitChangeDetector.GetLatestCommitHashAsync();

                // Assert
                Assert.Equal(string.Empty, result);
            }
            finally
            {
                Directory.SetCurrentDirectory(originalDir);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetChangedFilesAsync_ShouldHandleInvalidInput(string sinceReference)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _gitChangeDetector.GetChangedFilesAsync(sinceReference));
            
            Assert.Contains("sinceReference", exception.Message);
        }

        [Theory]
        [InlineData(null, "HEAD")]
        [InlineData("HEAD", null)]
        [InlineData("", "HEAD")]
        [InlineData("HEAD", "")]
        public async Task GetChangedFilesBetweenCommitsAsync_ShouldHandleInvalidInput(string fromCommit, string toCommit)
        {
            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => 
                _gitChangeDetector.GetChangedFilesBetweenCommitsAsync(fromCommit, toCommit));
            
            if (string.IsNullOrWhiteSpace(fromCommit))
            {
                Assert.Contains("fromCommit", exception.Message);
            }
            else if (string.IsNullOrWhiteSpace(toCommit))
            {
                Assert.Contains("toCommit", exception.Message);
            }
        }
    }
}