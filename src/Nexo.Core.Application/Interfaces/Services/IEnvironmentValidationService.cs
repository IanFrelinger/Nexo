using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.Onboarding
{
    /// <summary>
    /// Interface for environment validation services
    /// </summary>
    public interface IEnvironmentValidationService
    {
        Task<ValidationResult> ValidateEnvironmentAsync();
        Task<List<ValidationIssue>> GetValidationIssuesAsync();
        Task<bool> FixValidationIssueAsync(string issueId);
        Task<Dictionary<string, object>> GetEnvironmentInfoAsync();
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationIssue> Issues { get; set; } = new();
        public DateTime ValidatedAt { get; set; }
    }

    public class ValidationIssue
    {
        public string Id { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string FixSuggestion { get; set; } = string.Empty;
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
