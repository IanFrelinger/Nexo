using Nexo.Core.Domain.Behaviors;

namespace Nexo.Demo.Api.DTOs;

/// <summary>
/// Data transfer object for Behavior.
/// </summary>
public class BehaviorDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = default!;
    public string Description { get; init; } = default!;
    public IReadOnlyList<BehaviorStepDto> Steps { get; init; } = [];
    public string OnStepFailure { get; init; } = default!;
    public int MaxRetries { get; init; }
    public TimeSpan Timeout { get; init; }
    public IReadOnlyList<BehaviorParameterDto> Inputs { get; init; } = [];
    public IReadOnlyList<BehaviorParameterDto> Outputs { get; init; } = [];
    
    public static BehaviorDto FromDomain(Behavior behavior)
    {
        return new BehaviorDto
        {
            Id = behavior.Id,
            Name = behavior.Name,
            Version = behavior.Version,
            Description = behavior.Description,
            Steps = behavior.Steps.Select(s => new BehaviorStepDto
            {
                Id = s.Id,
                BrickId = s.BrickId,
                Implementation = s.Implementation.ToString().ToLowerInvariant(),
                InputMapping = s.InputMapping,
                OutputMapping = s.OutputMapping,
                Condition = s.Condition,
                Config = s.Config
            }).ToList(),
            OnStepFailure = behavior.OnStepFailure.ToString().ToLowerInvariant(),
            MaxRetries = behavior.MaxRetries,
            Timeout = behavior.Timeout,
            Inputs = behavior.Inputs.Select(i => new BehaviorParameterDto
            {
                Name = i.Name,
                Type = i.Type,
                Required = i.Required,
                Default = i.Default,
                Description = i.Description
            }).ToList(),
            Outputs = behavior.Outputs.Select(o => new BehaviorParameterDto
            {
                Name = o.Name,
                Type = o.Type,
                Required = o.Required,
                Default = o.Default,
                Description = o.Description
            }).ToList()
        };
    }
}

public class BehaviorStepDto
{
    public string Id { get; init; } = default!;
    public string BrickId { get; init; } = default!;
    public string Implementation { get; init; } = default!;
    public IReadOnlyDictionary<string, string> InputMapping { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> OutputMapping { get; init; } = new Dictionary<string, string>();
    public string? Condition { get; init; }
    public IReadOnlyDictionary<string, object>? Config { get; init; }
}

public class BehaviorParameterDto
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public bool Required { get; init; }
    public object? Default { get; init; }
    public string? Description { get; init; }
}

