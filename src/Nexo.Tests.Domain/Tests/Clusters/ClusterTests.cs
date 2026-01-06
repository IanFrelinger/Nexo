using Nexo.Core.Application.Testing.Abstractions;
using Nexo.Core.Application.Testing.Models;
using Nexo.Core.Domain.Behaviors;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;

namespace Nexo.Tests.Domain.Tests.Clusters;

/// <summary>
/// Smoke tests for Cluster domain models.
/// </summary>
public class ClusterTests : UnitTestBase
{
    public override async Task<TestResult> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await TestClusterCreation();
            await TestClusterBrick();
            await TestClusterConnection();
            await TestClusterInterface();
            await TestClusterParameter();
            await TestScalingConfig();
            await TestClusterInstance();
            
            return new TestResult
            {
                TestName = nameof(ClusterTests),
                Category = "Domain",
                Passed = true,
                Message = "All cluster smoke tests passed"
            };
        }
        catch (AssertionException ex)
        {
            return new TestResult
            {
                TestName = nameof(ClusterTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Assertion failed: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
        catch (Exception ex)
        {
            return new TestResult
            {
                TestName = nameof(ClusterTests),
                Category = "Domain",
                Passed = false,
                ErrorMessage = $"Unexpected exception: {ex.Message}",
                StackTrace = ex.StackTrace
            };
        }
    }
    
    private async Task TestClusterCreation()
    {
        var cluster = new Cluster
        {
            Id = "test-cluster",
            Name = "Test Cluster",
            Version = "1.0.0",
            Icon = "🔗",
            Description = "A test cluster",
            Bricks = [],
            Connections = [],
            Interface = new ClusterInterface(),
            Parameters = [],
            Scaling = new ScalingConfig(),
            Tags = ["test", "smoke"],
            Metadata = new ClusterMetadata
            {
                Author = "test-user",
                Scope = SharingScope.Private
            }
        };
        
        AssertEqual("test-cluster", cluster.Id);
        AssertEqual("Test Cluster", cluster.Name);
        AssertEqual(2, cluster.Tags.Count);
        AssertEqual("test-user", cluster.Metadata.Author);
        
        await Task.CompletedTask;
    }
    
    private async Task TestClusterBrick()
    {
        var clusterBrick = new ClusterBrick
        {
            BrickId = "test-brick",
            LocalId = "brick-1",
            Position = new Position(10, 20),
            DefaultImplementation = ImplementationType.Deterministic,
            AllowImplementationOverride = true,
            AllowParameterOverride = true,
            StaticParameters = new Dictionary<string, object> { ["key"] = "value" },
            ParameterMappings = new Dictionary<string, string> { ["input"] = "param" }
        };
        
        AssertEqual("test-brick", clusterBrick.BrickId);
        AssertEqual("brick-1", clusterBrick.LocalId);
        AssertEqual(10.0, clusterBrick.Position.X);
        AssertEqual(20.0, clusterBrick.Position.Y);
        AssertEqual(ImplementationType.Deterministic, clusterBrick.DefaultImplementation);
        AssertTrue(clusterBrick.AllowImplementationOverride);
        
        await Task.CompletedTask;
    }
    
    private async Task TestClusterConnection()
    {
        var connection = new ClusterConnection
        {
            Id = "conn-1",
            FromBrickId = "brick-1",
            FromOutput = "output",
            ToBrickId = "brick-2",
            ToInput = "input",
            Condition = "value > 0",
            Type = ConnectionType.Data
        };
        
        AssertEqual("conn-1", connection.Id);
        AssertEqual("brick-1", connection.FromBrickId);
        AssertEqual("brick-2", connection.ToBrickId);
        AssertEqual(ConnectionType.Data, connection.Type);
        
        await Task.CompletedTask;
    }
    
    private async Task TestClusterInterface()
    {
        var iface = new ClusterInterface
        {
            Inputs = [
                new ClusterPort
                {
                    Name = "input1",
                    Type = "string",
                    Description = "Test input",
                    Required = true,
                    InternalMapping = "brick-1.input"
                }
            ],
            Outputs = [
                new ClusterPort
                {
                    Name = "output1",
                    Type = "string",
                    Description = "Test output",
                    InternalMapping = "brick-2.output"
                }
            ],
            Events = [
                new ClusterEvent
                {
                    Name = "event1",
                    Description = "Test event",
                    InternalMapping = "brick-1.event"
                }
            ],
            FailurePolicy = FailurePolicy.Abort
        };
        
        AssertEqual(1, iface.Inputs.Count);
        AssertEqual(1, iface.Outputs.Count);
        AssertEqual(1, iface.Events.Count);
        AssertEqual(FailurePolicy.Abort, iface.FailurePolicy);
        
        await Task.CompletedTask;
    }
    
    private async Task TestClusterParameter()
    {
        var param = new ClusterParameter
        {
            Name = "count",
            DisplayName = "Count",
            Description = "Number of items",
            Type = "int",
            Default = 10,
            Min = 0,
            Max = 100,
            AllowedValues = null,
            UIHints = new ParameterUIHints
            {
                ControlType = "slider",
                Group = "Basic",
                Order = 1,
                Advanced = false
            }
        };
        
        AssertEqual("count", param.Name);
        AssertEqual("int", param.Type);
        AssertEqual(10, param.Default);
        AssertNotNull(param.UIHints);
        AssertEqual("slider", param.UIHints.ControlType);
        
        await Task.CompletedTask;
    }
    
    private async Task TestScalingConfig()
    {
        var scaling = new ScalingConfig
        {
            Mode = ScalingMode.Fixed,
            FixedCount = 5,
            MaxInstances = 100,
            MinInstances = 1,
            Distribution = new InstanceDistribution
            {
                Rules = [
                    new DistributionRule
                    {
                        Parameter = "id",
                        Type = DistributionType.Sequential
                    }
                ]
            }
        };
        
        AssertEqual(ScalingMode.Fixed, scaling.Mode);
        AssertEqual(5, scaling.FixedCount);
        AssertNotNull(scaling.Distribution);
        AssertEqual(1, scaling.Distribution.Rules.Count);
        
        await Task.CompletedTask;
    }
    
    private async Task TestClusterInstance()
    {
        var instance = new ClusterInstance
        {
            ClusterId = "test-cluster",
            InstanceId = "instance-1",
            Name = "Test Instance",
            Parameters = new Dictionary<string, object> { ["count"] = 5 },
            ImplementationOverrides = new Dictionary<string, ImplementationType>
            {
                ["brick-1"] = ImplementationType.Agentic
            },
            Position = new Position(100, 200)
        };
        
        AssertEqual("test-cluster", instance.ClusterId);
        AssertEqual("instance-1", instance.InstanceId);
        AssertEqual(1, instance.Parameters.Count);
        AssertEqual(1, instance.ImplementationOverrides.Count);
        AssertNotNull(instance.Position);
        
        await Task.CompletedTask;
    }
}

