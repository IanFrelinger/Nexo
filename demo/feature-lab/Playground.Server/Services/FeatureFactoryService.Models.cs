using System;
using System.Collections.Generic;

namespace Playground.Server.Services
{
    /// <summary>
    /// Data models for FeatureFactoryService.
    /// </summary>
    public partial class FeatureGenerationRequest
    {
        public string Description { get; set; } = string.Empty;
        public string Platform { get; set; } = ".NET";
        public List<string> Requirements { get; set; } = new();
    }

    public partial class FeatureGenerationResult
    {
        public string RequestId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double Duration { get; set; }
        public List<FeatureGenerationStep> Steps { get; set; } = new();
        public DomainAnalysis? DomainAnalysis { get; set; }
        public ArchitectureDecision? ArchitectureDecision { get; set; }
        public GeneratedCode? GeneratedCode { get; set; }
        public GeneratedTests? GeneratedTests { get; set; }
    }

    public partial class FeatureGenerationStep
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Description { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public partial class DomainAnalysis
    {
        public List<DomainEntity> Entities { get; set; } = new();
        public List<ValueObject> ValueObjects { get; set; } = new();
        public List<DomainService> DomainServices { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
    }

    public partial class DomainEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
    }

    public partial class ValueObject
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
    }

    public partial class DomainService
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public partial class BusinessRule
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public partial class ArchitectureDecision
    {
        public string Strategy { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public List<string> RecommendedPatterns { get; set; } = new();
        public List<string> PerformanceConsiderations { get; set; } = new();
        public List<string> SecurityConsiderations { get; set; } = new();
    }

    public partial class GeneratedCode
    {
        public List<string> Platforms { get; set; } = new();
        public List<GeneratedFile> Files { get; set; } = new();
    }

    public partial class GeneratedFile
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public partial class GeneratedTests
    {
        public List<string> UnitTests { get; set; } = new();
        public List<string> IntegrationTests { get; set; } = new();
    }
}
