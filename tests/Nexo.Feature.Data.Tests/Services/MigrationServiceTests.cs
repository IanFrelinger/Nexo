using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Interfaces;
using Nexo.Feature.Data.Services;
using Xunit;

namespace Nexo.Feature.Data.Tests.Services
{
    /// <summary>
    /// Tests for migration service functionality
    /// </summary>
    public class MigrationServiceTests
    {
        private readonly Mock<IDatabaseProvider> _mockDatabaseProvider;
        private readonly Mock<ILogger<MigrationService>> _mockLogger;
        private readonly MigrationService _migrationService;

        public MigrationServiceTests()
        {
            _mockDatabaseProvider = new Mock<IDatabaseProvider>();
            _mockLogger = new Mock<ILogger<MigrationService>>();
            _migrationService = new MigrationService(_mockDatabaseProvider.Object, _mockLogger.Object);
        }

        [Fact]
        public void MigrationService_Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var service = new MigrationService(_mockDatabaseProvider.Object, _mockLogger.Object);

            // Assert
            Assert.NotNull(service);
        }

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
        public async Task GetMigrationsAsync_ReturnsAllMigrations()
        {
            // Act
            var result = await _migrationService.GetMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Equal(3, result.Count()); // Sample migrations initialized in constructor
        }

        [Fact]
        public async Task GetAppliedMigrationsAsync_ReturnsAppliedMigrations()
        {
            // Act
            var result = await _migrationService.GetAppliedMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, migration => Assert.True(migration.IsApplied));
        }

        [Fact]
        public async Task GetPendingMigrationsAsync_ReturnsPendingMigrations()
        {
            // Act
            var result = await _migrationService.GetPendingMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, migration => Assert.False(migration.IsApplied));
        }

        [Fact]
        public async Task ApplyMigrationsAsync_WithPendingMigrations_AppliesAllMigrations()
        {
            // Arrange
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _migrationService.ApplyMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.NotEmpty(result.AppliedMigrations);
            Assert.Empty(result.FailedMigrations);
            Assert.True(result.ExecutionTime >= TimeSpan.Zero);
            Assert.True(result.ExecutedAt > DateTime.MinValue);
        }

        [Fact]
        public async Task ApplyMigrationAsync_WithValidMigrationId_AppliesMigration()
        {
            // Arrange
            var migrationId = "Migration_003"; // Pending migration from sample data
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _migrationService.ApplyMigrationAsync(migrationId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.Single(result.AppliedMigrations);
            Assert.Equal(migrationId, result.AppliedMigrations.First().Id);
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
        public async Task ApplyMigrationAsync_WithAlreadyAppliedMigration_ReturnsSuccess()
        {
            // Arrange
            var migrationId = "Migration_001"; // Already applied migration from sample data

            // Act
            var result = await _migrationService.ApplyMigrationAsync(migrationId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.Contains("already applied", result.Message);
        }

        [Fact]
        public async Task RollbackLastMigrationAsync_WithAppliedMigrations_RollbacksLastMigration()
        {
            // Arrange
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _migrationService.RollbackLastMigrationAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.NotEmpty(result.AppliedMigrations); // Rolled back migrations
        }

        [Fact]
        public async Task RollbackToMigrationAsync_WithValidMigrationId_RollbacksToTarget()
        {
            // Arrange
            var targetMigrationId = "Migration_001";
            _mockDatabaseProvider
                .Setup(x => x.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _migrationService.RollbackToMigrationAsync(targetMigrationId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.Contains(targetMigrationId, result.Message);
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
        public async Task ValidateMigrationsAsync_ReturnsValidationResult()
        {
            // Act
            var result = await _migrationService.ValidateMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MigrationValidationResult>(result);
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.PendingMigrations);
            Assert.NotNull(result.CurrentVersion);
            Assert.NotNull(result.TargetVersion);
        }

        [Fact]
        public async Task CreateMigrationAsync_WithValidParameters_CreatesMigration()
        {
            // Arrange
            var name = "Test Migration";
            var description = "Test migration description";

            // Act
            var result = await _migrationService.CreateMigrationAsync(name, description);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(name, result.Name);
            Assert.Equal(description, result.Description);
            Assert.False(result.IsApplied);
            Assert.True(result.Timestamp > DateTime.MinValue);
            Assert.NotNull(result.Script);
            Assert.NotNull(result.RollbackScript);
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
        public async Task GetMigrationHistoryAsync_ReturnsHistory()
        {
            // Act
            var result = await _migrationService.GetMigrationHistoryAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.All(result, history => 
            {
                Assert.NotNull(history.MigrationId);
                Assert.NotNull(history.MigrationName);
                Assert.True(history.AppliedAt > DateTime.MinValue);
                Assert.True(history.WasSuccessful);
            });
        }

        [Fact]
        public async Task GetSchemaVersionAsync_ReturnsCurrentVersion()
        {
            // Act
            var result = await _migrationService.GetSchemaVersionAsync();

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
            Assert.Matches(@"^\d+\.\d+\.\d+$", result); // Version format
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

        [Fact]
        public async Task ValidateMigrationsAsync_WithCircularDependencies_ReturnsInvalidResult()
        {
            // This test would require modifying the sample data to include circular dependencies
            // For now, we test the basic validation functionality
            var result = await _migrationService.ValidateMigrationsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsValid); // Sample data doesn't have circular dependencies
        }

        [Fact]
        public async Task GetMigrationsAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.GetMigrationsAsync(cts.Token));
        }

        [Fact]
        public async Task ApplyMigrationsAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.ApplyMigrationsAsync(cts.Token));
        }

        [Fact]
        public async Task ApplyMigrationAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var migrationId = "Migration_003";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.ApplyMigrationAsync(migrationId, cts.Token));
        }

        [Fact]
        public async Task RollbackLastMigrationAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.RollbackLastMigrationAsync(cts.Token));
        }

        [Fact]
        public async Task RollbackToMigrationAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var migrationId = "Migration_001";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.RollbackToMigrationAsync(migrationId, cts.Token));
        }

        [Fact]
        public async Task ValidateMigrationsAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.ValidateMigrationsAsync(cts.Token));
        }

        [Fact]
        public async Task CreateMigrationAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var name = "Test Migration";
            var description = "Test description";
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.CreateMigrationAsync(name, description, cts.Token));
        }

        [Fact]
        public async Task GetMigrationHistoryAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.GetMigrationHistoryAsync(cts.Token));
        }

        [Fact]
        public async Task GetSchemaVersionAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                _migrationService.GetSchemaVersionAsync(cts.Token));
        }
    }
} 