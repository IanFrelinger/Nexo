using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Commands
{
    /// <summary>
    /// Generates fixes for critical validation issues.
    /// </summary>
    public class CriticalIssueFixGenerator : BaseAutoFixGenerator
    {
        public override string Name => "CriticalIssueFixGenerator";

        public override bool CanHandle(ValidationIssue issue)
        {
            return issue.Severity == ValidationSeverity.Critical;
        }

        public override bool CanHandle(ValidationSuggestion suggestion)
        {
            return false;
        }

        public override async Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();

            // Critical issues require immediate attention
            if (issue.Category.ToLower().Contains("missing"))
            {
                fixes.Add(CreateFix(
                    $"Add Missing {issue.Title}",
                    $"Add the missing {issue.Title} to resolve critical issue: {issue.Description}",
                    ValidationSeverity.Critical,
                    "MissingComponent",
                    issue.Location,
                    true,
                    95,
                    "AddComponent",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "High",
                    riskLevel: "High",
                    tags: new[] { "critical", "missing" }
                ));
            }
            else if (issue.Category.ToLower().Contains("invalid"))
            {
                fixes.Add(CreateFix(
                    $"Fix Invalid {issue.Title}",
                    $"Fix the invalid {issue.Title}: {issue.Description}",
                    ValidationSeverity.Critical,
                    "InvalidComponent",
                    issue.Location,
                    true,
                    90,
                    "FixComponent",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "High",
                    riskLevel: "High",
                    tags: new[] { "critical", "invalid" }
                ));
            }
            else if (issue.Category.ToLower().Contains("error"))
            {
                fixes.Add(CreateFix(
                    $"Resolve {issue.Title} Error",
                    $"Resolve the error in {issue.Title}: {issue.Description}",
                    ValidationSeverity.Critical,
                    "Error",
                    issue.Location,
                    true,
                    85,
                    "FixError",
                    new Dictionary<string, object> { ["errorType"] = issue.Title },
                    estimatedEffort: "High",
                    riskLevel: "High",
                    tags: new[] { "critical", "error" }
                ));
            }

            await Task.Delay(10, cancellationToken); // Simulate async work
            return fixes;
        }

        public override Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AutoFix>>(new List<AutoFix>());
        }
    }

    /// <summary>
    /// Generates fixes for error validation issues.
    /// </summary>
    public class ErrorIssueFixGenerator : BaseAutoFixGenerator
    {
        public override string Name => "ErrorIssueFixGenerator";

        public override bool CanHandle(ValidationIssue issue)
        {
            return issue.Severity == ValidationSeverity.Error;
        }

        public override bool CanHandle(ValidationSuggestion suggestion)
        {
            return false;
        }

        public override async Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();

            // Error issues need to be fixed
            if (issue.Category.ToLower().Contains("performance"))
            {
                fixes.Add(CreateFix(
                    $"Optimize {issue.Title} Performance",
                    $"Optimize performance for {issue.Title}: {issue.Description}",
                    ValidationSeverity.Error,
                    "Performance",
                    issue.Location,
                    true,
                    80,
                    "OptimizePerformance",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Medium",
                    riskLevel: "Medium",
                    tags: new[] { "error", "performance" }
                ));
            }
            else if (issue.Category.ToLower().Contains("memory"))
            {
                fixes.Add(CreateFix(
                    $"Fix {issue.Title} Memory Issue",
                    $"Fix memory issue in {issue.Title}: {issue.Description}",
                    ValidationSeverity.Error,
                    "Memory",
                    issue.Location,
                    true,
                    85,
                    "FixMemory",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Medium",
                    riskLevel: "Medium",
                    tags: new[] { "error", "memory" }
                ));
            }
            else if (issue.Category.ToLower().Contains("validation"))
            {
                fixes.Add(CreateFix(
                    $"Fix {issue.Title} Validation",
                    $"Fix validation issue in {issue.Title}: {issue.Description}",
                    ValidationSeverity.Error,
                    "Validation",
                    issue.Location,
                    true,
                    75,
                    "FixValidation",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Medium",
                    riskLevel: "Low",
                    tags: new[] { "error", "validation" }
                ));
            }

            await Task.Delay(10, cancellationToken); // Simulate async work
            return fixes;
        }

        public override Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AutoFix>>(new List<AutoFix>());
        }
    }

    /// <summary>
    /// Generates fixes for warning validation issues.
    /// </summary>
    public class WarningIssueFixGenerator : BaseAutoFixGenerator
    {
        public override string Name => "WarningIssueFixGenerator";

        public override bool CanHandle(ValidationIssue issue)
        {
            return issue.Severity == ValidationSeverity.Warning;
        }

        public override bool CanHandle(ValidationSuggestion suggestion)
        {
            return false;
        }

        public override async Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();

            // Warning issues can be improved
            if (issue.Category.ToLower().Contains("quality"))
            {
                fixes.Add(CreateFix(
                    $"Improve {issue.Title} Quality",
                    $"Improve quality for {issue.Title}: {issue.Description}",
                    ValidationSeverity.Warning,
                    "Quality",
                    issue.Location,
                    true,
                    70,
                    "ImproveQuality",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Low",
                    riskLevel: "Low",
                    tags: new[] { "warning", "quality" }
                ));
            }
            else if (issue.Category.ToLower().Contains("optimization"))
            {
                fixes.Add(CreateFix(
                    $"Optimize {issue.Title}",
                    $"Optimize {issue.Title}: {issue.Description}",
                    ValidationSeverity.Warning,
                    "Optimization",
                    issue.Location,
                    true,
                    65,
                    "Optimize",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Low",
                    riskLevel: "Low",
                    tags: new[] { "warning", "optimization" }
                ));
            }
            else if (issue.Category.ToLower().Contains("style"))
            {
                fixes.Add(CreateFix(
                    $"Improve {issue.Title} Style",
                    $"Improve style for {issue.Title}: {issue.Description}",
                    ValidationSeverity.Warning,
                    "Style",
                    issue.Location,
                    true,
                    60,
                    "ImproveStyle",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Low",
                    riskLevel: "Low",
                    tags: new[] { "warning", "style" }
                ));
            }

            await Task.Delay(10, cancellationToken); // Simulate async work
            return fixes;
        }

        public override Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AutoFix>>(new List<AutoFix>());
        }
    }

    /// <summary>
    /// Generates fixes for info validation issues.
    /// </summary>
    public class InfoIssueFixGenerator : BaseAutoFixGenerator
    {
        public override string Name => "InfoIssueFixGenerator";

        public override bool CanHandle(ValidationIssue issue)
        {
            return issue.Severity == ValidationSeverity.Info;
        }

        public override bool CanHandle(ValidationSuggestion suggestion)
        {
            return false;
        }

        public override async Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationIssue issue, CancellationToken cancellationToken)
        {
            var fixes = new List<AutoFix>();

            // Info issues are suggestions for improvement
            if (issue.Category.ToLower().Contains("enhancement"))
            {
                fixes.Add(CreateFix(
                    $"Enhance {issue.Title}",
                    $"Enhance {issue.Title}: {issue.Description}",
                    ValidationSeverity.Info,
                    "Enhancement",
                    issue.Location,
                    false,
                    50,
                    "Enhance",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Low",
                    riskLevel: "Low",
                    tags: new[] { "info", "enhancement" }
                ));
            }
            else if (issue.Category.ToLower().Contains("feature"))
            {
                fixes.Add(CreateFix(
                    $"Add {issue.Title} Feature",
                    $"Add feature to {issue.Title}: {issue.Description}",
                    ValidationSeverity.Info,
                    "Feature",
                    issue.Location,
                    false,
                    45,
                    "AddFeature",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Medium",
                    riskLevel: "Low",
                    tags: new[] { "info", "feature" }
                ));
            }
            else
            {
                fixes.Add(CreateFix(
                    $"Improve {issue.Title}",
                    $"Improve {issue.Title}: {issue.Description}",
                    ValidationSeverity.Info,
                    "Improvement",
                    issue.Location,
                    false,
                    40,
                    "Improve",
                    new Dictionary<string, object> { ["componentType"] = issue.Title },
                    estimatedEffort: "Low",
                    riskLevel: "Low",
                    tags: new[] { "info", "improvement" }
                ));
            }

            await Task.Delay(10, cancellationToken); // Simulate async work
            return fixes;
        }

        public override Task<IReadOnlyList<AutoFix>> GenerateFixesAsync(ValidationSuggestion suggestion, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<AutoFix>>(new List<AutoFix>());
        }
    }
}
