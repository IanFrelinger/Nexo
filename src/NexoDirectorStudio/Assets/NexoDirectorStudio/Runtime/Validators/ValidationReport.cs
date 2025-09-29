using System.Text.Json;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Aggregates validation results from all validators into a comprehensive report.
    /// Provides overall validation status and gating for the Playtest button.
    /// </summary>
    public sealed class ValidationReport
    {
        /// <summary>
        /// Overall validation status.
        /// </summary>
        public ValidationStatus Status { get; init; }
        
        /// <summary>
        /// Overall validation score (0-100, where 100 is perfect).
        /// </summary>
        public int OverallScore { get; init; }
        
        /// <summary>
        /// Human-readable summary of the validation results.
        /// </summary>
        public string Summary { get; init; } = string.Empty;
        
        /// <summary>
        /// Detailed validation report.
        /// </summary>
        public string Details { get; init; } = string.Empty;
        
        /// <summary>
        /// All validation results from individual validators.
        /// </summary>
        public IReadOnlyList<ValidationResult> Results { get; init; } = Array.Empty<ValidationResult>();
        
        /// <summary>
        /// All issues found across all validators.
        /// </summary>
        public IReadOnlyList<ValidationIssue> AllIssues { get; init; } = Array.Empty<ValidationIssue>();
        
        /// <summary>
        /// All suggestions from all validators.
        /// </summary>
        public IReadOnlyList<ValidationSuggestion> AllSuggestions { get; init; } = Array.Empty<ValidationSuggestion>();
        
        /// <summary>
        /// Issues grouped by severity.
        /// </summary>
        public IReadOnlyDictionary<ValidationSeverity, IReadOnlyList<ValidationIssue>> IssuesBySeverity { get; init; } = new Dictionary<ValidationSeverity, IReadOnlyList<ValidationIssue>>();
        
        /// <summary>
        /// Issues grouped by category.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<ValidationIssue>> IssuesByCategory { get; init; } = new Dictionary<string, IReadOnlyList<ValidationIssue>>();
        
        /// <summary>
        /// Timestamp when the validation was performed.
        /// </summary>
        public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
        
        /// <summary>
        /// Whether the validation passed and playtest is allowed.
        /// </summary>
        public bool IsPlaytestAllowed => Status == ValidationStatus.Pass;
        
        /// <summary>
        /// Whether there are any critical issues that prevent playtesting.
        /// </summary>
        public bool HasCriticalIssues => AllIssues.Any(i => i.Severity == ValidationSeverity.Critical);
        
        /// <summary>
        /// Whether there are any errors that should be addressed.
        /// </summary>
        public bool HasErrors => AllIssues.Any(i => i.Severity == ValidationSeverity.Error);
        
        /// <summary>
        /// Whether there are any warnings that should be reviewed.
        /// </summary>
        public bool HasWarnings => AllIssues.Any(i => i.Severity == ValidationSeverity.Warning);
        
        /// <summary>
        /// Creates a validation report from individual validator results.
        /// </summary>
        public static ValidationReport Create(IReadOnlyList<ValidationResult> results)
        {
            if (results.Count == 0)
            {
                return new ValidationReport
                {
                    Status = ValidationStatus.Pass,
                    OverallScore = 100,
                    Summary = "No validators were run.",
                    Details = "No validation results available."
                };
            }
            
            // Calculate overall score
            var overallScore = results.Average(r => r.Score);
            
            // Determine overall status
            var status = DetermineOverallStatus(results);
            
            // Aggregate all issues and suggestions
            var allIssues = results.SelectMany(r => r.Issues).ToList();
            var allSuggestions = results.SelectMany(r => r.Suggestions).ToList();
            
            // Group issues by severity
            var issuesBySeverity = allIssues.GroupBy(i => i.Severity)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationIssue>)g.ToList());
            
            // Group issues by category
            var issuesByCategory = allIssues.GroupBy(i => i.Category)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationIssue>)g.ToList());
            
            // Generate summary
            var summary = GenerateSummary(status, overallScore, allIssues);
            
            // Generate details
            var details = GenerateDetails(results, allIssues, allSuggestions);
            
            return new ValidationReport
            {
                Status = status,
                OverallScore = (int)Math.Round(overallScore),
                Summary = summary,
                Details = details,
                Results = results,
                AllIssues = allIssues,
                AllSuggestions = allSuggestions,
                IssuesBySeverity = issuesBySeverity,
                IssuesByCategory = issuesByCategory
            };
        }
        
        /// <summary>
        /// Creates a validation report from a single validator result.
        /// </summary>
        public static ValidationReport Create(ValidationResult result)
        {
            return Create(new[] { result });
        }
        
        /// <summary>
        /// Creates an empty validation report.
        /// </summary>
        public static ValidationReport Empty()
        {
            return new ValidationReport
            {
                Status = ValidationStatus.Pass,
                OverallScore = 100,
                Summary = "No validation performed.",
                Details = "No validation results available."
            };
        }
        
        /// <summary>
        /// Serializes the validation report to JSON.
        /// </summary>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Serialize(this, options);
        }
        
        /// <summary>
        /// Deserializes a validation report from JSON.
        /// </summary>
        public static ValidationReport FromJson(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            
            return JsonSerializer.Deserialize<ValidationReport>(json, options) ?? Empty();
        }
        
        private static ValidationStatus DetermineOverallStatus(IReadOnlyList<ValidationResult> results)
        {
            // If any result is invalid, the overall status is fail
            if (results.Any(r => !r.IsValid))
            {
                return ValidationStatus.Fail;
            }
            
            // If any result has critical issues, the overall status is fail
            if (results.Any(r => r.Issues.Any(i => i.Severity == ValidationSeverity.Critical)))
            {
                return ValidationStatus.Fail;
            }
            
            // If any result has errors, the overall status is warning
            if (results.Any(r => r.Issues.Any(i => i.Severity == ValidationSeverity.Error)))
            {
                return ValidationStatus.Warning;
            }
            
            // If any result has warnings, the overall status is warning
            if (results.Any(r => r.Issues.Any(i => i.Severity == ValidationSeverity.Warning)))
            {
                return ValidationStatus.Warning;
            }
            
            // All results are valid with no issues
            return ValidationStatus.Pass;
        }
        
        private static string GenerateSummary(ValidationStatus status, double overallScore, IReadOnlyList<ValidationIssue> issues)
        {
            var criticalCount = issues.Count(i => i.Severity == ValidationSeverity.Critical);
            var errorCount = issues.Count(i => i.Severity == ValidationSeverity.Error);
            var warningCount = issues.Count(i => i.Severity == ValidationSeverity.Warning);
            var infoCount = issues.Count(i => i.Severity == ValidationSeverity.Info);
            
            return status switch
            {
                ValidationStatus.Pass => $"Validation passed with score {overallScore:F1}/100. No issues found.",
                ValidationStatus.Warning => $"Validation passed with warnings. Score: {overallScore:F1}/100. Issues: {criticalCount} critical, {errorCount} errors, {warningCount} warnings, {infoCount} info.",
                ValidationStatus.Fail => $"Validation failed. Score: {overallScore:F1}/100. Issues: {criticalCount} critical, {errorCount} errors, {warningCount} warnings, {infoCount} info.",
                _ => "Unknown validation status."
            };
        }
        
        private static string GenerateDetails(IReadOnlyList<ValidationResult> results, IReadOnlyList<ValidationIssue> issues, IReadOnlyList<ValidationSuggestion> suggestions)
        {
            var details = new List<string>
            {
                $"Validation Results: {results.Count}",
                $"Overall Score: {results.Average(r => r.Score):F1}/100",
                $"Total Issues: {issues.Count}",
                $"Total Suggestions: {suggestions.Count}"
            };
            
            if (issues.Count > 0)
            {
                details.Add("\nIssues by Severity:");
                foreach (var severity in Enum.GetValues<ValidationSeverity>())
                {
                    var severityIssues = issues.Where(i => i.Severity == severity).ToList();
                    if (severityIssues.Count > 0)
                    {
                        details.Add($"  {severity}: {severityIssues.Count}");
                    }
                }
                
                details.Add("\nIssues by Category:");
                var categoryGroups = issues.GroupBy(i => i.Category);
                foreach (var group in categoryGroups)
                {
                    details.Add($"  {group.Key}: {group.Count()}");
                }
            }
            
            if (suggestions.Count > 0)
            {
                details.Add("\nSuggestions by Priority:");
                var priorityGroups = suggestions.GroupBy(s => s.Priority);
                foreach (var group in priorityGroups.OrderByDescending(g => g.Key))
                {
                    details.Add($"  Priority {group.Key}: {group.Count()}");
                }
            }
            
            return string.Join("\n", details);
        }
    }
    
    /// <summary>
    /// Overall validation status.
    /// </summary>
    public enum ValidationStatus
    {
        /// <summary>
        /// Validation passed with no issues.
        /// </summary>
        Pass,
        
        /// <summary>
        /// Validation passed with warnings.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Validation failed with errors or critical issues.
        /// </summary>
        Fail
    }
}
