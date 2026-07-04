using Nexo.Brick.Contracts;
using Nexo.Brick.Contracts.Capabilities;
using Nexo.Core.Application.NodeCapabilityRuntime.Ports;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;

namespace Nexo.API;

/// <summary>DomainBrick catalog wire mapper.</summary>
internal static class BrickCatalogWireMapper
{
    /// <summary>Maps a domain brick and host capabilities to a catalog wire DTO.</summary>
    public static BrickCatalogEntryDto ToEntry(DomainBrick brick, NodeCapabilityManifestDto? hostCapabilities)
    {
        return new BrickCatalogEntryDto
        {
            Id = brick.Id,
            Name = brick.Name,
            Version = brick.Version,
            Icon = brick.Icon,
            Category = brick.Category.ToString(),
            Description = brick.Description,
            HostBaseUrl = null,
            Interface = new BrickInterfaceDto
            {
                Inputs = brick.Interface.Inputs.Select(i => new BrickInputDefinitionDto
                {
                    Name = i.Name,
                    Type = i.Type,
                    Description = i.Description,
                    Required = i.Required,
                    Default = i.Default
                }).ToList(),
                Outputs = brick.Interface.Outputs.Select(o => new BrickOutputDefinitionDto
                {
                    Name = o.Name,
                    Type = o.Type,
                    Description = o.Description
                }).ToList()
            },
            HasDeterministic = brick.Implementations.Deterministic != null,
            HasAgentic = brick.Implementations.Agentic != null,
            Metadata = new BrickMetadataDto
            {
                Author = brick.Metadata.Author,
                License = brick.Metadata.License,
                Repository = brick.Metadata.Repository,
                UsageCount = brick.Metadata.UsageCount,
                LastUpdated = brick.Metadata.LastUpdated
            },
            DomainKnowledge = ToDomainKnowledgeDto(brick.DomainKnowledge),
            HostCapabilities = hostCapabilities
        };
    }

    private static DomainKnowledgeDto? ToDomainKnowledgeDto(DomainKnowledge? dk)
    {
        if (dk is null) return null;
        return new DomainKnowledgeDto
        {
            Standards = dk.Standards.ToList(),
            Rules = dk.Rules.Select(r => new DomainRuleDto
            {
                Id = r.Id,
                Description = r.Description,
                Pattern = r.Pattern,
                Severity = r.Severity,
                CweId = r.CweId
            }).ToList(),
            ReferenceData = new Dictionary<string, string>(dk.ReferenceData),
            LearnedPatterns = dk.LearnedPatterns.Select(p => new LearnedPatternDto
            {
                Pattern = p.Pattern,
                Outcome = p.Outcome,
                Confidence = p.Confidence,
                OccurrenceCount = p.OccurrenceCount
            }).ToList(),
            ExecutionCount = dk.ExecutionCount
        };
    }

    /// <summary>Maps an execution context DTO to the infrastructure execution context model.</summary>
    public static Nexo.Infrastructure.Execution.ExecutionContext ToExecutionContext(ExecutionContextDto? dto)
    {
        if (dto is null)
            return new Nexo.Infrastructure.Execution.ExecutionContext();

        return new Nexo.Infrastructure.Execution.ExecutionContext
        {
            AgentId = dto.AgentId ?? string.Empty,
            BehaviorId = dto.BehaviorId ?? string.Empty,
            IsAirGapped = dto.IsAirGapped,
            AuditMode = dto.AuditMode,
            Provider = dto.Provider ?? "openai",
            Variables = dto.Variables != null
                ? new Dictionary<string, object>(dto.Variables)
                : new Dictionary<string, object>()
        };
    }
}
