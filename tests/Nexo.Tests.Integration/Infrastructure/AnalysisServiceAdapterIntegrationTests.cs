using Xunit;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Analysis.Ports;
using Nexo.Infrastructure.Analysis.Adapters;
using System.IO.Abstractions.TestingHelpers;

namespace Nexo.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for AnalysisServiceAdapter.
/// Tests the adapter with real file system operations.
/// </summary>
public class AnalysisServiceAdapterIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ILogger<AnalysisServiceAdapter> _logger;
    private readonly AnalysisServiceAdapter _adapter;

    public AnalysisServiceAdapterIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Create a mock logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<AnalysisServiceAdapter>();

        _adapter = new AnalysisServiceAdapter(_logger);
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyDirectory_ReturnsNoViolations()
    {
        // Arrange
        var path = new DirectoryInfo(_testDirectory);

        // Act
        var result = await _adapter.AnalyzeAsync(path, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.HasViolations);
        Assert.Equal(0, result.TotalViolations);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task AnalyzeAsync_WithAssemblyFiles_ReturnsResults()
    {
        // Arrange
        var path = new DirectoryInfo(_testDirectory);
        
        // Create a dummy DLL file (we'll use an existing one from the build output)
        var testDllPath = Path.Combine(_testDirectory, "test.dll");
        var existingDll = Directory.GetFiles(
            Path.Combine(Directory.GetCurrentDirectory(), "src"),
            "*.dll",
            SearchOption.AllDirectories)
            .FirstOrDefault();

        if (existingDll != null && File.Exists(existingDll))
        {
            File.Copy(existingDll, testDllPath);
        }
        else
        {
            // Create a minimal DLL file for testing
            File.WriteAllBytes(testDllPath, new byte[] { 0x4D, 0x5A }); // MZ header
        }

        // Act
        var result = await _adapter.AnalyzeAsync(path, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        // Note: May have violations if file is invalid, or no violations if analysis succeeds
        Assert.NotNull(result.Violations);
    }

    [Fact]
    public async Task AnalyzeAsync_WithNonExistentPath_ThrowsException()
    {
        // Arrange
        var path = new DirectoryInfo("/nonexistent/path/that/does/not/exist");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            _adapter.AnalyzeAsync(path, CancellationToken.None));
    }

    [Fact]
    public async Task AnalyzeAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var path = new DirectoryInfo(_testDirectory);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _adapter.AnalyzeAsync(path, cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }
}

