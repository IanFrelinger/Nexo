using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Feature.Data.Contexts;
using Nexo.Feature.Data.Enums;
using Xunit;

namespace Nexo.Feature.Data.Tests.Contexts;

/// <summary>
/// Tests for the NexoDbContext
/// </summary>
public class NexoDbContextTests : IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<NexoDbContext>> _mockLogger;

    public NexoDbContextTests()
    {
        // Create a real configuration object
        var configurationData = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "UseInMemoryDatabase"
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();
        
        _mockLogger = new Mock<ILogger<NexoDbContext>>();
    }

    [Fact]
    public void Constructor_WithValidOptions_ShouldCreateInstance()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act
        var context = new NexoDbContext(options, _configuration, _mockLogger.Object);

        // Assert
        Assert.NotNull(context);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NexoDbContext(options, null!, _mockLogger.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new NexoDbContext(options, _configuration, null!));
    }

    [Fact]
    public void Provider_ShouldReturnCorrectProvider()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new NexoDbContext(options, _configuration, _mockLogger.Object);

        // Act
        var provider = context.Provider;

        // Assert
        Assert.Equal(DatabaseProvider.InMemory, provider);
    }

    [Fact]
    public async Task EnsureDatabaseCreatedAsync_ShouldCreateDatabase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new NexoDbContext(options, _configuration, _mockLogger.Object);

        // Act
        await context.EnsureDatabaseCreatedAsync();

        // Assert
        Assert.True(context.Database.CanConnect());
    }

    [Fact]
    public async Task TestConnectionAsync_ValidConnection_ShouldReturnTrue()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new NexoDbContext(options, _configuration, _mockLogger.Object);

        // Act
        var result = await context.TestConnectionAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetStatisticsAsync_ShouldReturnValidStatistics()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<NexoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new NexoDbContext(options, _configuration, _mockLogger.Object);

        // Act
        var result = await context.GetStatisticsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(DatabaseProvider.InMemory, result.Provider);
        Assert.True(result.IsOnline);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
} 