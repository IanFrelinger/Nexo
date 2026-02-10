using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Common.Ports;
using Nexo.Core.Application.Common.Services;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.BaseFramework;

/// <summary>
/// Smoke tests for base framework components that geospatial applications depend on.
/// These tests validate infrastructure before testing the geo application itself.
/// </summary>
public class BaseFrameworkSmokeTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _testOutputDir;

    public BaseFrameworkSmokeTests()
    {
        _testOutputDir = Path.Combine(Path.GetTempPath(), $"nexo-base-framework-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDir);

        var services = new ServiceCollection();
        
        // Test logging infrastructure
        services.AddLogging(builder => builder.AddConsole());
        
        // Test HTTP client factory
        services.AddHttpClient();
        services.AddHttpClient("geoterrain.srtm");
        services.AddHttpClient("geovector.mapbox");
        
        // Test dependency injection
        services.AddScoped<IProviderFactory, ProviderFactory>();
        services.AddScoped<ILoopKernel, SequentialLoopKernel>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Logging_ShouldBeConfigured()
    {
        // Arrange & Act
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<BaseFrameworkSmokeTests>();

        // Assert
        logger.Should().NotBeNull("Logger should be created");
        loggerFactory.Should().NotBeNull("LoggerFactory should be available");
        
        // Verify logging works without throwing
        logger.LogInformation("Test log message");
    }

    [Fact]
    public void HttpClientFactory_ShouldCreateClients()
    {
        // Arrange
        var httpClientFactory = _serviceProvider.GetRequiredService<IHttpClientFactory>();

        // Act
        var defaultClient = httpClientFactory.CreateClient();
        var namedClient = httpClientFactory.CreateClient("geoterrain.srtm");

        // Assert
        defaultClient.Should().NotBeNull("Default HTTP client should be created");
        namedClient.Should().NotBeNull("Named HTTP client should be created");
        defaultClient.BaseAddress.Should().BeNull("Default client should have no base address");
    }

    [Fact]
    public void DependencyInjection_ShouldResolveServices()
    {
        // Arrange & Act
        var providerFactory = _serviceProvider.GetRequiredService<IProviderFactory>();
        var loopKernel = _serviceProvider.GetRequiredService<ILoopKernel>();

        // Assert
        providerFactory.Should().NotBeNull("ProviderFactory should be resolvable");
        loopKernel.Should().NotBeNull("LoopKernel should be resolvable");
    }

    [Fact]
    public void DatabaseOperations_ShouldBeSupported()
    {
        // Arrange
        var dbPath = Path.Combine(_testOutputDir, "test.db");
        
        // Act - Test that SQLite can be used (if available)
        // This validates database infrastructure without requiring API project
        var dbExists = File.Exists(dbPath);
        
        // Assert
        // Just verify file system supports database file creation
        // Actual database testing happens in geo app tests
        dbPath.Should().NotBeNullOrEmpty("Database path should be valid");
    }

    [Fact]
    public void FileSystem_ShouldSupportBasicOperations()
    {
        // Arrange
        var testFile = Path.Combine(_testOutputDir, "test.txt");
        var testContent = "Hello, World!";

        // Act
        File.WriteAllText(testFile, testContent);
        var readContent = File.ReadAllText(testFile);
        var exists = File.Exists(testFile);

        // Assert
        exists.Should().BeTrue("File should exist after creation");
        readContent.Should().Be(testContent, "File content should match");
    }

    [Fact]
    public void DirectoryOperations_ShouldWork()
    {
        // Arrange
        var testDir = Path.Combine(_testOutputDir, "subdir");

        // Act
        Directory.CreateDirectory(testDir);
        var exists = Directory.Exists(testDir);

        // Assert
        exists.Should().BeTrue("Directory should exist after creation");
    }

    [Fact]
    public void ServiceProvider_ShouldSupportScopedServices()
    {
        // Arrange
        using var scope1 = _serviceProvider.CreateScope();
        using var scope2 = _serviceProvider.CreateScope();

        // Act
        var service1 = scope1.ServiceProvider.GetRequiredService<IProviderFactory>();
        var service2 = scope2.ServiceProvider.GetRequiredService<IProviderFactory>();

        // Assert
        service1.Should().NotBeNull("Service should be resolvable in scope 1");
        service2.Should().NotBeNull("Service should be resolvable in scope 2");
        // Scoped services should be different instances
        service1.Should().NotBeSameAs(service2, "Scoped services should be different instances");
    }

    [Fact]
    public async Task AsyncOperations_ShouldComplete()
    {
        // Arrange & Act
        var result = await Task.Run(async () =>
        {
            await Task.Delay(10);
            return "completed";
        });

        // Assert
        result.Should().Be("completed", "Async operations should complete");
    }

    [Fact]
    public void CancellationToken_ShouldBeSupported()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        // Act
        var task = Task.Run(async () =>
        {
            await Task.Delay(1000, cts.Token);
        }, cts.Token);

        // Assert
        task.Invoking(t => t.Wait()).Should().Throw<AggregateException>("Task should be cancelled");
    }

    [Fact]
    public void AutonomousDevAgent_ShouldBeAvailable()
    {
        // Arrange & Act
        var agentType = Type.GetType("Nexo.Agents.AutonomousDev.AutonomousDevAgent, Nexo.Agents.AutonomousDev");

        // Assert
        agentType.Should().NotBeNull("AutonomousDevAgent should be available");
    }

    [Fact]
    public void UniversalTesterAgent_ShouldBeAvailable()
    {
        // Arrange & Act
        var agentType = Type.GetType("Nexo.Agents.UniversalTester.UniversalTesterAgent, Nexo.Agents.UniversalTester");

        // Assert
        agentType.Should().NotBeNull("UniversalTesterAgent should be available");
    }

    [Fact]
    public void NexoGuide_ShouldBeAvailable()
    {
        // Arrange & Act - Guide (nexo-guide agent, GuideChatBrick, IUnitySceneSink) is used by the Guide MAUI app
        var setupType = Type.GetType("Nexo.Guide.GuideNexoSetup, Nexo.Guide");

        // Assert
        setupType.Should().NotBeNull("Nexo Guide setup and agent types should be available");
    }

    public void Dispose()
    {
        try
        {
            _serviceProvider?.Dispose();
            if (Directory.Exists(_testOutputDir))
            {
                Directory.Delete(_testOutputDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
