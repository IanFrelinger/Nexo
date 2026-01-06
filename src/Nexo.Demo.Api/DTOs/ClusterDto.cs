using Nexo.Core.Domain.Clusters;

namespace Nexo.Demo.Api.DTOs;

/// <summary>
/// Data transfer object for Cluster summary (list view).
/// </summary>
public class ClusterSummaryDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Icon { get; init; } = default!;
    public string Description { get; init; } = default!;
    public int BrickCount { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public string Author { get; init; } = default!;
    public string Scope { get; init; } = default!;
    public int UsageCount { get; init; }
}

/// <summary>
/// Data transfer object for Cluster detail (full view).
/// </summary>
public class ClusterDetailDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = default!;
    public string Icon { get; init; } = default!;
    public string Description { get; init; } = default!;
    public IReadOnlyList<ClusterBrickDto> Bricks { get; init; } = [];
    public IReadOnlyList<ClusterConnectionDto> Connections { get; init; } = [];
    public ClusterInterfaceDto Interface { get; init; } = new();
    public IReadOnlyList<ClusterParameterDto> Parameters { get; init; } = [];
    public ScalingConfigDto Scaling { get; init; } = new();
    public IReadOnlyList<string> Tags { get; init; } = [];
    public ClusterMetadataDto Metadata { get; init; } = new();
    
    public static ClusterDetailDto FromDomain(Cluster cluster)
    {
        return new ClusterDetailDto
        {
            Id = cluster.Id,
            Name = cluster.Name,
            Version = cluster.Version,
            Icon = cluster.Icon,
            Description = cluster.Description,
            Bricks = cluster.Bricks.Select(b => new ClusterBrickDto
            {
                BrickId = b.BrickId,
                LocalId = b.LocalId,
                X = b.Position.X,
                Y = b.Position.Y,
                DefaultImplementation = b.DefaultImplementation.ToString().ToLowerInvariant(),
                AllowImplementationOverride = b.AllowImplementationOverride,
                AllowParameterOverride = b.AllowParameterOverride
            }).ToList(),
            Connections = cluster.Connections.Select(c => new ClusterConnectionDto
            {
                Id = c.Id,
                FromBrickId = c.FromBrickId,
                FromOutput = c.FromOutput,
                ToBrickId = c.ToBrickId,
                ToInput = c.ToInput,
                Condition = c.Condition,
                Type = c.Type.ToString().ToLowerInvariant()
            }).ToList(),
            Interface = new ClusterInterfaceDto
            {
                Inputs = cluster.Interface.Inputs.Select(i => new ClusterPortDto
                {
                    Name = i.Name,
                    Type = i.Type,
                    Description = i.Description,
                    Required = i.Required,
                    Default = i.Default,
                    InternalMapping = i.InternalMapping
                }).ToList(),
                Outputs = cluster.Interface.Outputs.Select(o => new ClusterPortDto
                {
                    Name = o.Name,
                    Type = o.Type,
                    Description = o.Description,
                    Required = o.Required,
                    Default = o.Default,
                    InternalMapping = o.InternalMapping
                }).ToList(),
                FailurePolicy = cluster.Interface.FailurePolicy.ToString().ToLowerInvariant()
            },
            Parameters = cluster.Parameters.Select(p => new ClusterParameterDto
            {
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Type = p.Type,
                Default = p.Default,
                Min = p.Min,
                Max = p.Max,
                AllowedValues = p.AllowedValues
            }).ToList(),
            Scaling = new ScalingConfigDto
            {
                Mode = cluster.Scaling.Mode.ToString().ToLowerInvariant(),
                FixedCount = cluster.Scaling.FixedCount,
                DynamicExpression = cluster.Scaling.DynamicExpression,
                MaxInstances = cluster.Scaling.MaxInstances,
                MinInstances = cluster.Scaling.MinInstances
            },
            Tags = cluster.Tags,
            Metadata = new ClusterMetadataDto
            {
                Author = cluster.Metadata.Author,
                CreatedAt = cluster.Metadata.CreatedAt,
                ModifiedAt = cluster.Metadata.ModifiedAt,
                Scope = cluster.Metadata.Scope.ToString().ToLowerInvariant(),
                Stats = new ClusterStatsDto
                {
                    UsageCount = cluster.Metadata.Stats.UsageCount,
                    FavoriteCount = cluster.Metadata.Stats.FavoriteCount,
                    AverageRating = cluster.Metadata.Stats.AverageRating
                }
            }
        };
    }
}

public class ClusterBrickDto
{
    public string BrickId { get; init; } = default!;
    public string LocalId { get; init; } = default!;
    public double X { get; init; }
    public double Y { get; init; }
    public string DefaultImplementation { get; init; } = default!;
    public bool AllowImplementationOverride { get; init; }
    public bool AllowParameterOverride { get; init; }
}

public class ClusterConnectionDto
{
    public string Id { get; init; } = default!;
    public string FromBrickId { get; init; } = default!;
    public string FromOutput { get; init; } = default!;
    public string ToBrickId { get; init; } = default!;
    public string ToInput { get; init; } = default!;
    public string? Condition { get; init; }
    public string Type { get; init; } = default!;
}

public class ClusterInterfaceDto
{
    public IReadOnlyList<ClusterPortDto> Inputs { get; init; } = [];
    public IReadOnlyList<ClusterPortDto> Outputs { get; init; } = [];
    public string FailurePolicy { get; init; } = default!;
}

public class ClusterPortDto
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string Description { get; init; } = default!;
    public bool Required { get; init; }
    public object? Default { get; init; }
    public string InternalMapping { get; init; } = default!;
}

public class ClusterParameterDto
{
    public string Name { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Type { get; init; } = default!;
    public object? Default { get; init; }
    public object? Min { get; init; }
    public object? Max { get; init; }
    public IReadOnlyList<string>? AllowedValues { get; init; }
}

public class ScalingConfigDto
{
    public string Mode { get; init; } = default!;
    public int FixedCount { get; init; }
    public string? DynamicExpression { get; init; }
    public int MaxInstances { get; init; }
    public int MinInstances { get; init; }
}

public class ClusterMetadataDto
{
    public string Author { get; init; } = default!;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
    public string Scope { get; init; } = default!;
    public ClusterStatsDto Stats { get; init; } = new();
}

public class ClusterStatsDto
{
    public int UsageCount { get; init; }
    public int FavoriteCount { get; init; }
    public double AverageRating { get; init; }
}

public class ClusterInstanceDto
{
    public string InstanceId { get; init; } = default!;
    public string ClusterId { get; init; } = default!;
    public string? Name { get; init; }
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
    public IReadOnlyDictionary<string, string> ImplementationOverrides { get; init; } = new Dictionary<string, string>();
}

