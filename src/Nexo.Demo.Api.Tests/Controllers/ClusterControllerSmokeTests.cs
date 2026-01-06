using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Demo.Api.Controllers;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Api.Tests.Controllers;

/// <summary>
/// Smoke tests for ClusterController.
/// </summary>
public class ClusterControllerSmokeTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestGetAllClusters();
            await TestGetCluster();
            await TestCreateCluster();
            await TestCreateInstance();
            
            return new TestResult
            {
                TestName = nameof(ClusterControllerSmokeTests),
                Category = "API",
                Passed = true,
                Message = "All ClusterController smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ClusterControllerSmokeTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(ClusterControllerSmokeTests),
                Category = "API",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestGetAllClusters()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockClusterExecutor = new Mock<IClusterExecutor>();
        
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test Cluster",
            Description = "Test",
            Icon = "🔗",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Tags = ["test"],
            Metadata = new ClusterMetadata
            {
                Author = "test-user",
                Scope = SharingScope.Private,
                Stats = new ClusterStats { UsageCount = 5 }
            }
        };
        
        mockClusterRegistry.Setup(r => r.GetAll()).Returns([cluster]);
        
        var controller = new ClusterController(
            mockClusterRegistry.Object,
            mockClusterExecutor.Object);
        
        var result = controller.GetAll();
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        var clusters = okResult.Value as IEnumerable<object>;
        AssertNotNull(clusters);
        
        await Task.CompletedTask;
    }
    
    private async Task TestGetCluster()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockClusterExecutor = new Mock<IClusterExecutor>();
        
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
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        
        var controller = new ClusterController(
            mockClusterRegistry.Object,
            mockClusterExecutor.Object);
        
        var result = controller.Get("test-cluster");
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
    
    private async Task TestCreateCluster()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockClusterExecutor = new Mock<IClusterExecutor>();
        
        var controller = new ClusterController(
            mockClusterRegistry.Object,
            mockClusterExecutor.Object);
        
        var request = new CreateClusterRequest(
            "New Cluster",
            "Description",
            "🔗",
            [],
            [],
            [],
            [],
            [],
            "single",
            1,
            100,
            [],
            "private"
        );
        
        var result = controller.Create(request);
        var createdResult = result.Result as CreatedAtActionResult;
        
        AssertNotNull(createdResult);
        AssertEqual(201, createdResult.StatusCode);
        
        mockClusterRegistry.Verify(r => r.Register(It.IsAny<Cluster>()), Times.Once);
        
        await Task.CompletedTask;
    }
    
    private async Task TestCreateInstance()
    {
        var mockClusterRegistry = new Mock<IClusterRegistry>();
        var mockClusterExecutor = new Mock<IClusterExecutor>();
        
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
        
        mockClusterRegistry.Setup(r => r.Get("test-cluster")).Returns(cluster);
        
        var controller = new ClusterController(
            mockClusterRegistry.Object,
            mockClusterExecutor.Object);
        
        var request = new CreateInstanceRequest(
            "Instance 1",
            new Dictionary<string, object> { ["count"] = 5 },
            new Dictionary<string, string>()
        );
        
        var result = controller.CreateInstance("test-cluster", request);
        var okResult = result.Result as OkObjectResult;
        
        AssertNotNull(okResult);
        AssertEqual(200, okResult.StatusCode);
        
        await Task.CompletedTask;
    }
}

