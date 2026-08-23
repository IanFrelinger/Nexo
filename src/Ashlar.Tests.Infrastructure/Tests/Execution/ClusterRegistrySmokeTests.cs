using Ashlar.Core.Application.Testing.Abstractions;
using Ashlar.Core.Application.Testing.Models;
using Ashlar.Core.Domain.Clusters;
using Ashlar.Infrastructure.Execution;

namespace Ashlar.Tests.Infrastructure.Tests.Execution;

/// <summary>
/// Smoke tests for ClusterRegistry.
/// </summary>
public class ClusterRegistrySmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestRegisterAndGet();
            await TestGetAll();
            await TestUnregister();
            
            return new TestResult
            {
                Name = nameof(ClusterRegistrySmokeTests),
                Category = "Infrastructure",
                Passed = true,
                Message = "All ClusterRegistry smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                Name = nameof(ClusterRegistrySmokeTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                Name = nameof(ClusterRegistrySmokeTests),
                Category = "Infrastructure",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestRegisterAndGet()
    {
        var registry = new ClusterRegistry();
        
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Metadata = new ClusterMetadata { Author = "test" }
        };
        
        registry.Register(cluster);
        
        var retrieved = registry.Get("test-cluster");
        AssertNotNull(retrieved);
        AssertEqual("test-cluster", retrieved!.Id);
        
        var notFound = registry.Get("missing");
        AssertNull(notFound);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetAll()
    {
        var cluster1 = new Cluster
        {
            Id = "cluster-1",
            Name = "Cluster 1",
            Description = "Test",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Metadata = new ClusterMetadata { Author = "test" }
        };
        
        var cluster2 = new Cluster
        {
            Id = "cluster-2",
            Name = "Cluster 2",
            Description = "Test",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Metadata = new ClusterMetadata { Author = "test" }
        };
        
        var registry = new ClusterRegistry([cluster1, cluster2]);
        
        var all = registry.GetAll();
        AssertEqual(2, all.Count);
        AssertTrue(all.Any(c => c != null && c.Id == "cluster-1"));
        AssertTrue(all.Any(c => c != null && c.Id == "cluster-2"));
        
        await Task.CompletedTask;
    }
    
    private async Task TestUnregister()
    {
        var registry = new ClusterRegistry();
        
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test",
            Description = "Test",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Metadata = new ClusterMetadata { Author = "test" }
        };
        
        registry.Register(cluster);
        AssertNotNull(registry.Get("test-cluster"));
        
        registry.Unregister("test-cluster");
        AssertNull(registry.Get("test-cluster"));
        
        await Task.CompletedTask;
    }
}

