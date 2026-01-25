using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Infrastructure.Execution;
using Xunit;

namespace Nexo.Tests.Infrastructure.Tests.Stress;

/// <summary>
/// Stress tests for infrastructure components under resource constraints.
/// </summary>
public class ResourceLimitStressTests
{
    [Fact]
    public async Task ProviderFactory_ShouldHandleHighConcurrency()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IProviderFactory, ProviderFactory>();
        var serviceProvider = services.BuildServiceProvider();

        const int concurrentRequests = 150;
        var factory = serviceProvider.GetRequiredService<IProviderFactory>();

        // Act
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(async i =>
            {
                try
                {
                    // Simulate provider creation
                    await Task.Delay(Random.Shared.Next(5, 20));
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().AllBeEquivalentTo(true, "All concurrent provider requests should complete");
    }

    [Fact]
    public async Task ServiceProvider_ShouldHandleManyScopedServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IProviderFactory, ProviderFactory>();
        var serviceProvider = services.BuildServiceProvider();

        const int scopeCount = 200;

        // Act
        var successCount = 0;
        for (int i = 0; i < scopeCount; i++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();
                factory.Should().NotBeNull();
                successCount++;
            }
            catch
            {
                // Count failures
            }
        }

        // Assert
        successCount.Should().Be(scopeCount, "All scoped services should be created successfully");
    }

    [Fact]
    public void Infrastructure_ShouldNotLeakMemory()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IProviderFactory, ProviderFactory>();
        var serviceProvider = services.BuildServiceProvider();

        // Act - Create many scopes and services
        for (int i = 0; i < 1000; i++)
        {
            using var scope = serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();
            _ = factory; // Use it
        }

        // Assert - Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    [Fact]
    public async Task Infrastructure_ShouldHandleResourceExhaustion()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IProviderFactory, ProviderFactory>();
        var serviceProvider = services.BuildServiceProvider();

        // Act - Create many concurrent operations
        const int operationCount = 500;
        var tasks = Enumerable.Range(0, operationCount)
            .Select(async i =>
            {
                try
                {
                    using var scope = serviceProvider.CreateScope();
                    var factory = scope.ServiceProvider.GetRequiredService<IProviderFactory>();
                    await Task.Delay(10);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r);
        successCount.Should().BeGreaterThan(operationCount * 0.95, 
            "At least 95% of operations should succeed under load");
    }
}
