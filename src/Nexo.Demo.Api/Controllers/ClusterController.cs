using Microsoft.AspNetCore.Mvc;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Clusters;
using Nexo.Demo.Api.DTOs;
using Nexo.Infrastructure.Execution;

namespace Nexo.Demo.Api.Controllers;

/// <summary>
/// REST API controller for cluster management.
/// </summary>
[ApiController]
[Route("api/clusters")]
public class ClusterController : ControllerBase
{
    private readonly IClusterRegistry _clusterRegistry;
    private readonly IClusterExecutor _clusterExecutor;
    
    public ClusterController(
        IClusterRegistry clusterRegistry,
        IClusterExecutor clusterExecutor)
    {
        _clusterRegistry = clusterRegistry;
        _clusterExecutor = clusterExecutor;
    }
    
    /// <summary>
    /// Get all clusters with optional filtering.
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<ClusterSummaryDto>> GetAll(
        [FromQuery] string? tag = null,
        [FromQuery] string? scope = null)
    {
        var clusters = _clusterRegistry.GetAll().AsEnumerable();
        
        if (tag is not null)
            clusters = clusters.Where(c => c.Tags.Contains(tag));
        
        if (scope is not null && Enum.TryParse<SharingScope>(scope, ignoreCase: true, out var scopeEnum))
            clusters = clusters.Where(c => c.Metadata.Scope == scopeEnum);
        
        return Ok(clusters.Select(c => new ClusterSummaryDto
        {
            Id = c.Id,
            Name = c.Name,
            Icon = c.Icon,
            Description = c.Description,
            BrickCount = c.Bricks.Count,
            Tags = c.Tags,
            Author = c.Metadata.Author,
            Scope = c.Metadata.Scope.ToString().ToLowerInvariant(),
            UsageCount = c.Metadata.Stats.UsageCount
        }));
    }
    
    /// <summary>
    /// Get a specific cluster by ID.
    /// </summary>
    [HttpGet("{id}")]
    public ActionResult<ClusterDetailDto> Get(string id)
    {
        var cluster = _clusterRegistry.Get(id);
        if (cluster is null) return NotFound();
        
        return Ok(ClusterDetailDto.FromDomain(cluster));
    }
    
    /// <summary>
    /// Create a new cluster.
    /// </summary>
    [HttpPost]
    public ActionResult<ClusterDetailDto> Create([FromBody] CreateClusterRequest request)
    {
        var cluster = new Cluster
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.Name,
            Description = request.Description,
            Icon = request.Icon ?? "🔗",
            Bricks = request.Bricks.Select(b => new ClusterBrick
            {
                BrickId = b.BrickId,
                LocalId = b.LocalId,
                Position = new Position(b.X, b.Y),
                DefaultImplementation = Enum.Parse<ImplementationType>(b.DefaultImplementation, ignoreCase: true),
                AllowImplementationOverride = b.AllowImplementationOverride
            }).ToList(),
            Connections = request.Connections.Select(c => new ClusterConnection
            {
                Id = Guid.NewGuid().ToString(),
                FromBrickId = c.FromBrickId,
                FromOutput = c.FromOutput,
                ToBrickId = c.ToBrickId,
                ToInput = c.ToInput,
                Condition = c.Condition,
                Type = Enum.Parse<ConnectionType>(c.Type, ignoreCase: true)
            }).ToList(),
            Interface = new ClusterInterface
            {
                Inputs = request.Inputs.Select(i => new ClusterPort
                {
                    Name = i.Name,
                    Type = i.Type,
                    Description = i.Description,
                    Required = i.Required,
                    InternalMapping = i.InternalMapping
                }).ToList(),
                Outputs = request.Outputs.Select(o => new ClusterPort
                {
                    Name = o.Name,
                    Type = o.Type,
                    Description = o.Description,
                    InternalMapping = o.InternalMapping
                }).ToList()
            },
            Parameters = request.Parameters.Select(p => new ClusterParameter
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Type = p.Type,
                Default = p.Default,
                Min = p.Min,
                Max = p.Max
            }).ToList(),
            Scaling = new ScalingConfig
            {
                Mode = Enum.Parse<ScalingMode>(request.ScalingMode, ignoreCase: true),
                FixedCount = request.FixedCount ?? 1,
                MaxInstances = request.MaxInstances ?? 100
            },
            Tags = request.Tags,
            Metadata = new ClusterMetadata
            {
                Author = User.Identity?.Name ?? "anonymous",
                Scope = Enum.TryParse<SharingScope>(request.Scope ?? "private", ignoreCase: true, out var s) ? s : SharingScope.Private
            }
        };
        
        _clusterRegistry.Register(cluster);
        
        return CreatedAtAction(nameof(Get), new { id = cluster.Id }, ClusterDetailDto.FromDomain(cluster));
    }
    
    /// <summary>
    /// Create a cluster instance.
    /// </summary>
    [HttpPost("{id}/instances")]
    public ActionResult<ClusterInstanceDto> CreateInstance(
        string id,
        [FromBody] CreateInstanceRequest request)
    {
        var cluster = _clusterRegistry.Get(id);
        if (cluster is null) return NotFound();
        
        var instance = new ClusterInstance
        {
            ClusterId = id,
            InstanceId = Guid.NewGuid().ToString(),
            Name = request.Name,
            Parameters = request.Parameters,
            ImplementationOverrides = request.ImplementationOverrides
                .ToDictionary(kv => kv.Key, kv => Enum.Parse<ImplementationType>(kv.Value, ignoreCase: true))
        };
        
        return Ok(new ClusterInstanceDto
        {
            InstanceId = instance.InstanceId,
            ClusterId = instance.ClusterId,
            Name = instance.Name,
            Parameters = instance.Parameters,
            ImplementationOverrides = instance.ImplementationOverrides
                .ToDictionary(kv => kv.Key, kv => kv.Value.ToString().ToLowerInvariant())
        });
    }
}

/// <summary>
/// Request to create a cluster.
/// </summary>
public record CreateClusterRequest(
    string Name,
    string Description,
    string? Icon,
    IReadOnlyList<CreateClusterBrickRequest> Bricks,
    IReadOnlyList<CreateClusterConnectionRequest> Connections,
    IReadOnlyList<CreateClusterPortRequest> Inputs,
    IReadOnlyList<CreateClusterPortRequest> Outputs,
    IReadOnlyList<CreateClusterParameterRequest> Parameters,
    string ScalingMode,
    int? FixedCount,
    int? MaxInstances,
    IReadOnlyList<string> Tags,
    string? Scope
);

public record CreateClusterBrickRequest(
    string BrickId,
    string LocalId,
    double X,
    double Y,
    string DefaultImplementation,
    bool AllowImplementationOverride
);

public record CreateClusterConnectionRequest(
    string FromBrickId,
    string FromOutput,
    string ToBrickId,
    string ToInput,
    string? Condition,
    string Type
);

public record CreateClusterPortRequest(
    string Name,
    string Type,
    string Description,
    bool Required,
    string InternalMapping
);

public record CreateClusterParameterRequest(
    string Name,
    string DisplayName,
    string Type,
    object? Default,
    object? Min,
    object? Max
);

/// <summary>
/// Request to create a cluster instance.
/// </summary>
public record CreateInstanceRequest(
    string? Name,
    Dictionary<string, object> Parameters,
    Dictionary<string, string> ImplementationOverrides
);

