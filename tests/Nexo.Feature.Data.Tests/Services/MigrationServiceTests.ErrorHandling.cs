using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Error handling test cases for MigrationService.
    /// </summary>
    public partial class MigrationServiceTests
    {
        [Fact]
        public void MigrationService_Constructor_WithNullDatabaseProvider_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MigrationService(null, _mockLogger.Object));
        }

        [Fact]
        public void MigrationService_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new MigrationService(_mockDatabaseProvider.Object, null));
        }

        [Fact]
        public async Task ApplyMigrationAsync_WithNonExistentMigrationId_ThrowsArgumentException()
        {
            // Arrange
            var migrationId = "NonExistentMigration";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _migrationService.ApplyMigrationAsync(migrationId));
        }

        [Fact]
        public async Task RollbackToMigrationAsync_WithNonExistentMigrationId_ThrowsArgumentException()
        {
            // Arrange
            var migrationId = "NonExistentMigration";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _migrationService.RollbackToMigrationAsync(migrationId));
        }

        [Fact]
        public async Task CreateMigrationAsync_WithEmptyName_ThrowsException()
        {
            // Arrange
            var name = "";
            var description = "Test description";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _migrationService.CreateMigrationAsync(name, description));
        }

        [Fact]
        public async Task ApplyMigrationsAsync_WithDatabaseError_ReturnsFailedResult()
        {
            // Arrange
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _migrationService.ApplyMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccessful);
            Assert.Empty(result.AppliedMigrations);
            Assert.NotEmpty(result.FailedMigrations);
            Assert.Contains("Database error", result.Message);
        }

        [Fact]
        public async Task ApplyMigrationAsync_WithDatabaseError_ReturnsFailedResult()
        {
            // Arrange
            var migrationId = "Migration_003";
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _migrationService.ApplyMigrationAsync(migrationId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsSuccessful);
            Assert.Empty(result.AppliedMigrations);
            Assert.NotEmpty(result.FailedMigrations);
            Assert.Contains("Database error", result.Message);
        }
    }
}
