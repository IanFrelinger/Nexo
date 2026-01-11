using Nexo.Core.Domain.Bricks;

namespace Nexo.Demo.Api.DTOs;

/// <summary>
/// Data transfer object for Brick.
/// </summary>
public class BrickDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Version { get; init; } = default!;
    public string Icon { get; init; } = default!;
    public string Category { get; init; } = default!;
    public string Description { get; init; } = default!;
    public DomainKnowledgeDto DomainKnowledge { get; init; } = new();
    public BrickInterfaceDto Interface { get; init; } = new();
    public BrickImplementationsDto Implementations { get; init; } = new();
    public string DefaultImplementation { get; init; } = default!;
    public BrickMetadataDto Metadata { get; init; } = new();
    
    public static BrickDto FromDomain(Brick brick)
    {
        return new BrickDto
        {
            Id = brick.Id,
            Name = brick.Name,
            Version = brick.Version,
            Icon = brick.Icon,
            Category = brick.Category.ToString(),
            Description = brick.Description,
            DomainKnowledge = DomainKnowledgeDto.FromDomain(brick.DomainKnowledge),
            Interface = BrickInterfaceDto.FromDomain(brick.Interface),
            Implementations = BrickImplementationsDto.FromDomain(brick.Implementations),
            DefaultImplementation = brick.DefaultImplementation.ToString().ToLowerInvariant(),
            Metadata = BrickMetadataDto.FromDomain(brick.Metadata)
        };
    }
}

public class DomainKnowledgeDto
{
    public IReadOnlyList<string> Standards { get; init; } = [];
    public IReadOnlyList<DomainRuleDto> Rules { get; init; } = [];
    public IReadOnlyDictionary<string, string> ReferenceData { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<LearnedPatternDto> LearnedPatterns { get; init; } = [];
    public long ExecutionCount { get; init; }
    
    public static DomainKnowledgeDto FromDomain(DomainKnowledge knowledge)
    {
        return new DomainKnowledgeDto
        {
            Standards = knowledge.Standards,
            Rules = knowledge.Rules.Select(r => new DomainRuleDto
            {
                Id = r.Id,
                Description = r.Description,
                Pattern = r.Pattern,
                Severity = r.Severity,
                CweId = r.CweId
            }).ToList(),
            ReferenceData = knowledge.ReferenceData,
            LearnedPatterns = knowledge.LearnedPatterns.Select(p => new LearnedPatternDto
            {
                Pattern = p.Pattern,
                Outcome = p.Outcome,
                Confidence = p.Confidence,
                OccurrenceCount = p.OccurrenceCount
            }).ToList(),
            ExecutionCount = knowledge.ExecutionCount
        };
    }
}

public class DomainRuleDto
{
    public string Id { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string? Pattern { get; init; }
    public string? Severity { get; init; }
    public string? CweId { get; init; }
}

public class LearnedPatternDto
{
    public string Pattern { get; init; } = default!;
    public string Outcome { get; init; } = default!;
    public double Confidence { get; init; }
    public int OccurrenceCount { get; init; }
}

public class BrickInterfaceDto
{
    public IReadOnlyList<BrickInputDefinitionDto> Inputs { get; init; } = [];
    public IReadOnlyList<BrickOutputDefinitionDto> Outputs { get; init; } = [];
    
    public static BrickInterfaceDto FromDomain(BrickInterface iface)
    {
        return new BrickInterfaceDto
        {
            Inputs = iface.Inputs.Select(i => new BrickInputDefinitionDto
            {
                Name = i.Name,
                Type = i.Type,
                Description = i.Description,
                Required = i.Required,
                Default = i.Default
            }).ToList(),
            Outputs = iface.Outputs.Select(o => new BrickOutputDefinitionDto
            {
                Name = o.Name,
                Type = o.Type,
                Description = o.Description
            }).ToList()
        };
    }
}

public class BrickInputDefinitionDto
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Description { get; init; }
    public bool Required { get; init; }
    public object? Default { get; init; }
}

public class BrickOutputDefinitionDto
{
    public string Name { get; init; } = default!;
    public string Type { get; init; } = default!;
    public string? Description { get; init; }
}

public class BrickImplementationsDto
{
    public DeterministicImplementationDto? Deterministic { get; init; }
    public AgenticImplementationDto? Agentic { get; init; }
    
    public static BrickImplementationsDto FromDomain(BrickImplementations impls)
    {
        return new BrickImplementationsDto
        {
            Deterministic = impls.Deterministic != null ? new DeterministicImplementationDto
            {
                Id = impls.Deterministic.Id,
                Name = impls.Deterministic.Name,
                Description = impls.Deterministic.Description,
                Executor = impls.Deterministic.Executor,
                Config = impls.Deterministic.Config,
                Characteristics = new ImplementationCharacteristicsDto
                {
                    Latency = impls.Deterministic.Characteristics.Latency,
                    Deterministic = impls.Deterministic.Characteristics.Deterministic,
                    RequiresNetwork = impls.Deterministic.Characteristics.RequiresNetwork,
                    ResourceUsage = impls.Deterministic.Characteristics.ResourceUsage.ToString().ToLowerInvariant()
                }
            } : null,
            Agentic = impls.Agentic != null ? new AgenticImplementationDto
            {
                Id = impls.Agentic.Id,
                Name = impls.Agentic.Name,
                Description = impls.Agentic.Description,
                Executor = impls.Agentic.Executor,
                LLMConfig = new LLMConfigDto
                {
                    Model = impls.Agentic.LLMConfig.Model,
                    SystemPrompt = impls.Agentic.LLMConfig.SystemPrompt,
                    Temperature = impls.Agentic.LLMConfig.Temperature,
                    MaxTokens = impls.Agentic.LLMConfig.MaxTokens,
                    Tools = impls.Agentic.LLMConfig.Tools
                },
                ProviderMappings = impls.Agentic.ProviderMappings.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ProviderConfigDto
                    {
                        Model = kvp.Value.Model,
                        Endpoint = kvp.Value.Endpoint
                    }),
                Characteristics = new ImplementationCharacteristicsDto
                {
                    Latency = impls.Agentic.Characteristics.Latency,
                    Deterministic = impls.Agentic.Characteristics.Deterministic,
                    RequiresNetwork = impls.Agentic.Characteristics.RequiresNetwork,
                    ResourceUsage = impls.Agentic.Characteristics.ResourceUsage.ToString().ToLowerInvariant()
                }
            } : null
        };
    }
}

public class DeterministicImplementationDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Executor { get; init; } = default!;
    public IReadOnlyDictionary<string, object> Config { get; init; } = new Dictionary<string, object>();
    public ImplementationCharacteristicsDto Characteristics { get; init; } = new();
}

public class AgenticImplementationDto
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Executor { get; init; } = default!;
    public LLMConfigDto LLMConfig { get; init; } = new();
    public IReadOnlyDictionary<string, ProviderConfigDto> ProviderMappings { get; init; } = new Dictionary<string, ProviderConfigDto>();
    public ImplementationCharacteristicsDto Characteristics { get; init; } = new();
}

public class LLMConfigDto
{
    public string Model { get; init; } = default!;
    public string SystemPrompt { get; init; } = default!;
    public double Temperature { get; init; }
    public int MaxTokens { get; init; }
    public IReadOnlyList<string>? Tools { get; init; }
}

public class ProviderConfigDto
{
    public string Model { get; init; } = default!;
    public string? Endpoint { get; init; }
}

public class ImplementationCharacteristicsDto
{
    public string Latency { get; init; } = default!;
    public bool Deterministic { get; init; }
    public bool RequiresNetwork { get; init; }
    public string ResourceUsage { get; init; } = default!;
}

public class BrickMetadataDto
{
    public string? Author { get; init; }
    public string? License { get; init; }
    public string? Repository { get; init; }
    public long UsageCount { get; init; }
    public DateTime? LastUpdated { get; init; }
    
    public static BrickMetadataDto FromDomain(BrickMetadata metadata)
    {
        return new BrickMetadataDto
        {
            Author = metadata.Author,
            License = metadata.License,
            Repository = metadata.Repository,
            UsageCount = metadata.UsageCount,
            LastUpdated = metadata.LastUpdated
        };
    }
}

