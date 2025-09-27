using System;
using System.Collections.Generic;

namespace Playground.Server.Services
{
    /// <summary>
    /// Data models for FeatureFactoryService.
    /// </summary>
    public class FeatureGenerationRequest
    {
        public string Description { get; set; } = string.Empty;
        public string Platform { get; set; } = ".NET";
        public List<string> Requirements { get; set; } = new();
    }

    public class FeatureGenerationResult
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

    public class FeatureGenerationStep
    {
        public string StepName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Description { get; set; } = string.Empty;
        public string Output { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class DomainAnalysis
    {
        public List<DomainEntity> Entities { get; set; } = new();
        public List<ValueObject> ValueObjects { get; set; } = new();
        public List<DomainService> DomainServices { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
    }

    public class DomainEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
    }

    public class ValueObject
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Properties { get; set; } = new();
    }

    public class DomainService
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class BusinessRule
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ArchitectureDecision
    {
        public string Strategy { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public List<string> RecommendedPatterns { get; set; } = new();
        public List<string> PerformanceConsiderations { get; set; } = new();
        public List<string> SecurityConsiderations { get; set; } = new();
    }

    public class GeneratedCode
    {
        public List<string> Platforms { get; set; } = new();
        public List<GeneratedFile> Files { get; set; } = new();
    }

    public class GeneratedFile
    {
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class GeneratedTests
    {
        public List<string> UnitTests { get; set; } = new();
        public List<string> IntegrationTests { get; set; } = new();
    }
}
