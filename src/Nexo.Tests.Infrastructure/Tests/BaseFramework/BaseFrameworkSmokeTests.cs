using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Nexo.API.Models;
using Nexo.API.Services;
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
        
        // Test job repository (SQLite)
        var dbPath = Path.Combine(_testOutputDir, "test-jobs.db");
        services.AddSingleton<IJobRepository>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SqliteJobRepository>>();
            return new SqliteJobRepository(dbPath, logger);
        });

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
    public void JobRepository_ShouldCreateAndRetrieveJobs()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IJobRepository>();
        var jobId = Guid.NewGuid().ToString("N");
        var job = new JobStatusResponse
        {
            JobId = jobId,
            Status = "pending",
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var createdId = repository.CreateJobAsync(job).Result;
        var retrieved = repository.GetJobAsync(jobId).Result;

        // Assert
        createdId.Should().Be(jobId, "Job ID should be returned");
        retrieved.Should().NotBeNull("Job should be retrievable");
        retrieved!.JobId.Should().Be(jobId, "Retrieved job should match created job");
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
    public void AsyncOperations_ShouldComplete()
    {
        // Arrange
        var repository = _serviceProvider.GetRequiredService<IJobRepository>();

        // Act
        var task = Task.Run(async () =>
        {
            await Task.Delay(10);
            return "completed";
        });

        var result = task.Result;

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
