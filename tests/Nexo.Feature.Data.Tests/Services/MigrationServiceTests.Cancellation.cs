using System;
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
    /// Cancellation test cases for MigrationService.
    /// </summary>
    public partial class MigrationServiceTests
    {
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
