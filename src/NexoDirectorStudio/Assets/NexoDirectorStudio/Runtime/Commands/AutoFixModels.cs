using System;
using System.Collections.Generic;
using System.Linq;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Represents a single auto-fix that can be applied to resolve a validation issue.
    /// </summary>
    public sealed record AutoFix
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Title { get; init; } = "";
        public string Description { get; init; } = "";
        public ValidationSeverity Severity { get; init; }
        public string Category { get; init; } = "";
        public string Location { get; init; } = "";
        public bool CanApplyAutomatically { get; init; }
        public int ConfidenceScore { get; init; } // 0-100
        public string FixType { get; init; } = "";
        public Dictionary<string, object> Parameters { get; init; } = new();
        public string[] Dependencies { get; init; } = Array.Empty<string>();
        public string[] Conflicts { get; init; } = Array.Empty<string>();
        public string EstimatedEffort { get; init; } = "";
        public string RiskLevel { get; init; } = "";
        public string[] Tags { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Collection of auto-fix proposals with metadata.
    /// </summary>
    public sealed record AutoFixProposal
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
        public IReadOnlyList<AutoFix> Suggestions { get; init; } = new List<AutoFix>();
        public bool CanApplyAllFixes { get; init; }
        public float AverageConfidence { get; init; }
        public string Summary { get; init; } = "";
        public string Description { get; init; } = "";
        public string EstimatedEffort { get; init; } = "";
        public int TotalFixes { get; init; }
        public int AutomaticFixes { get; init; }
        public int ManualFixes { get; init; }
        public Dictionary<string, int> FixesByCategory { get; init; } = new();
        public Dictionary<ValidationSeverity, int> FixesBySeverity { get; init; } = new();
        public string[] RecommendedOrder { get; init; } = Array.Empty<string>();
        public string[] Warnings { get; init; } = Array.Empty<string>();
        public string[] Notes { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Interface for auto-fix generators.
    /// </summary>
    public interface IAutoFixGenerator
    {
        string Name { get; }
        bool CanHandle(ValidationIssue issue);
        bool CanHandle(ValidationSuggestion suggestion);
        Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken);
        Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Base class for auto-fix generators.
    /// </summary>
    public abstract class BaseAutoFixGenerator : IAutoFixGenerator
    {
        public abstract string Name { get; }
        public abstract bool CanHandle(ValidationIssue issue);
        public abstract bool CanHandle(ValidationSuggestion suggestion);
        public abstract Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken);
        public abstract Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken);

        protected AutoFix CreateFix(
            string title,
            string description,
            ValidationSeverity severity,
            string category,
            string location,
            bool canApplyAutomatically,
            int confidenceScore,
            string fixType,
            Dictionary<string, object>? parameters = null,
            string[]? dependencies = null,
            string[]? conflicts = null,
            string estimatedEffort = "Low",
            string riskLevel = "Low",
            string[]? tags = null)
        {
            return new AutoFix
            {
                Title = title,
                Description = description,
                Severity = severity,
                Category = category,
                Location = location,
                CanApplyAutomatically = canApplyAutomatically,
                ConfidenceScore = confidenceScore,
                FixType = fixType,
                Parameters = parameters ?? new Dictionary<string, object>(),
                Dependencies = dependencies ?? Array.Empty<string>(),
                Conflicts = conflicts ?? Array.Empty<string>(),
                EstimatedEffort = estimatedEffort,
                RiskLevel = riskLevel,
                Tags = tags ?? Array.Empty<string>()
            };
        }
    }
}
