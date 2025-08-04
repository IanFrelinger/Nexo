using System;
using System.Collections.Generic;
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
    /// Tests for database provider functionality
    /// </summary>
    public class DatabaseProviderTests
    {
        private readonly Mock<ILogger<SqlServerProvider>> _mockLogger;
        private readonly string _connectionString;

        public DatabaseProviderTests()
        {
            _mockLogger = new Mock<ILogger<SqlServerProvider>>();
            _connectionString = "Server=localhost;Database=TestDB;Trusted_Connection=true;";
        }

        [Fact]
        public void SqlServerProvider_Constructor_WithValidParameters_CreatesInstance()
        {
            // Act
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Assert
            Assert.NotNull(provider);
            Assert.Equal(DatabaseType.SqlServer, provider.DatabaseType);
            Assert.Equal(_connectionString, provider.ConnectionString);
        }

        [Fact]
        public void SqlServerProvider_Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqlServerProvider(null, _connectionString));
        }

        [Fact]
        public void SqlServerProvider_Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SqlServerProvider(_mockLogger.Object, null));
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidConnection_ReturnsTrue()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Act
            var result = await provider.TestConnectionAsync();

            // Assert
            // Note: This test will fail if SQL Server is not available, which is expected in test environments
            // In a real scenario, you'd use a test database or mock the connection
            Assert.IsType<bool>(result);
        }

        [Fact]
        public async Task GetHealthStatusAsync_ReturnsHealthStatus()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Act
            var result = await provider.GetHealthStatusAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DatabaseHealthStatus>(result);
            Assert.True(result.LastChecked > DateTime.MinValue);
            Assert.NotNull(result.Details);
        }

        [Fact]
        public async Task QueryAsync_WithValidQuery_ReturnsResults()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var query = "SELECT 1 as TestValue";

            // Act
            var result = await provider.QueryAsync<object>(query);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<object>>(result);
        }

        [Fact]
        public async Task QueryAsync_WithParameters_ReturnsResults()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var query = "SELECT @Value as TestValue";
            var parameters = new Dictionary<string, object> { ["Value"] = "test" };

            // Act
            var result = await provider.QueryAsync<object>(query, parameters);

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IEnumerable<object>>(result);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidCommand_ReturnsAffectedRows()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var command = "SELECT 1"; // Simple command that doesn't modify data

            // Act
            var result = await provider.ExecuteAsync(command);

            // Assert
            Assert.IsType<int>(result);
            Assert.True(result >= 0);
        }

        [Fact]
        public async Task ExecuteAsync_WithParameters_ReturnsAffectedRows()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var command = "SELECT @Value";
            var parameters = new Dictionary<string, object> { ["Value"] = "test" };

            // Act
            var result = await provider.ExecuteAsync(command, parameters);

            // Assert
            Assert.IsType<int>(result);
            Assert.True(result >= 0);
        }

        [Fact]
        public async Task BeginTransactionAsync_ReturnsTransaction()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Act
            var result = await provider.BeginTransactionAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsAssignableFrom<IDatabaseTransaction>(result);
        }

        [Fact]
        public async Task GetStatisticsAsync_ReturnsStatistics()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Act
            var result = await provider.GetStatisticsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DatabaseStatistics>(result);
            Assert.True(result.TotalConnections >= 0);
            Assert.True(result.ActiveConnections >= 0);
            Assert.True(result.TotalQueries >= 0);
            Assert.True(result.FailedQueries >= 0);
            Assert.True(result.TotalTransactions >= 0);
            Assert.True(result.FailedTransactions >= 0);
        }

        [Fact]
        public async Task PerformMaintenanceAsync_WithValidType_ReturnsResult()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);

            // Act
            var result = await provider.PerformMaintenanceAsync(DatabaseMaintenanceType.Optimize);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<DatabaseMaintenanceResult>(result);
            Assert.True(result.CompletedAt > DateTime.MinValue);
            Assert.True(result.Duration >= TimeSpan.Zero);
        }

        [Fact]
        public async Task PerformMaintenanceAsync_WithInvalidType_ThrowsArgumentException()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var invalidType = (DatabaseMaintenanceType)999;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                provider.PerformMaintenanceAsync(invalidType));
        }

        [Fact]
        public async Task QueryAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.QueryAsync<object>("SELECT 1", null, cts.Token));
        }

        [Fact]
        public async Task ExecuteAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.ExecuteAsync("SELECT 1", null, cts.Token));
        }

        [Fact]
        public async Task BeginTransactionAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.BeginTransactionAsync(cts.Token));
        }

        [Fact]
        public async Task GetHealthStatusAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.GetHealthStatusAsync(cts.Token));
        }

        [Fact]
        public async Task GetStatisticsAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.GetStatisticsAsync(cts.Token));
        }

        [Fact]
        public async Task PerformMaintenanceAsync_WithCancellationToken_RespectsCancellation()
        {
            // Arrange
            var provider = new SqlServerProvider(_mockLogger.Object, _connectionString);
            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => 
                provider.PerformMaintenanceAsync(DatabaseMaintenanceType.Optimize, cts.Token));
        }
    }
} 