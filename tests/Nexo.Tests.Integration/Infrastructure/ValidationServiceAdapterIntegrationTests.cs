using Xunit;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Validation.Ports;
using Nexo.Infrastructure.Validation.Adapters;

namespace Nexo.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for ValidationServiceAdapter.
/// Tests the adapter with real test project discovery.
/// </summary>
public class ValidationServiceAdapterIntegrationTests
{
    private readonly ILogger<ValidationServiceAdapter> _logger;
    private readonly ValidationServiceAdapter _adapter;

    public ValidationServiceAdapterIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<ValidationServiceAdapter>();
        _adapter = new ValidationServiceAdapter(_logger);
    }

    [Fact]
    public async Task ValidateAsync_WithNoFilter_ReturnsResult()
    {
        // Arrange
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            // Change to a directory that might have test projects
            var testProjectsDir = Path.Combine(Directory.GetCurrentDirectory(), "tests");
            if (Directory.Exists(testProjectsDir))
            {
                Directory.SetCurrentDirectory(testProjectsDir);
            }

            // Act
            var result = await _adapter.ValidateAsync(null, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Message);
            // Result may pass or fail depending on whether test projects exist
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithFilter_ReturnsResult()
    {
        // Arrange
        var filter = "Category=Unit";

        // Act
        var result = await _adapter.ValidateAsync(filter, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _adapter.ValidateAsync(null, cancellationTokenSource.Token));
    }
}

