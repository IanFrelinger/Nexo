using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Infrastructure.Safety
{
    /// <summary>
    /// Safety rule implementations for EnhancedSafetyValidator.
    /// </summary>
    public partial class EnhancedSafetyValidator
    {
        // This partial class contains the safety rule implementations
    }

    /// <summary>
    /// Base class for safety rules
    /// </summary>
    public abstract class SafetyRule
    {
        public abstract Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken);
    }

    /// <summary>
    /// File system safety rule
    /// </summary>
    public class FileSystemSafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for dangerous file operations
            if (Regex.IsMatch(code, @"File\.Delete\s*\(\s*[""'][^""']*[""']", RegexOptions.IgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.FileSystemAccess,
                    Severity = SafetySeverity.Critical,
                    Message = "File deletion without validation",
                    Recommendation = "Validate file paths and implement proper authorization"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }

    /// <summary>
    /// Network safety rule
    /// </summary>
    public class NetworkSafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for insecure network operations
            if (Regex.IsMatch(code, @"HttpClient.*http://", RegexOptions.IgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.NetworkAccess,
                    Severity = SafetySeverity.Medium,
                    Message = "Insecure HTTP connection",
                    Recommendation = "Use HTTPS for secure communications"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }

    /// <summary>
    /// System command safety rule
    /// </summary>
    public class SystemCommandSafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for dangerous system commands
            if (code.Contains("Process.Start", StringComparison.OrdinalIgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.SystemCommand,
                    Severity = SafetySeverity.Critical,
                    Message = "Dangerous system command execution",
                    Recommendation = "Avoid executing system commands or implement proper sandboxing"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }

    /// <summary>
    /// Data destruction safety rule
    /// </summary>
    public class DataDestructionSafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for data destruction patterns
            if (Regex.IsMatch(code, @"DELETE\s+FROM\s+\w+", RegexOptions.IgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.DataDestruction,
                    Severity = SafetySeverity.Critical,
                    Message = "SQL DELETE without WHERE clause",
                    Recommendation = "Implement proper data validation and backup mechanisms"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }

    /// <summary>
    /// Security vulnerability safety rule
    /// </summary>
    public class SecurityVulnerabilitySafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for SQL injection
            if (Regex.IsMatch(code, @"string\.Format.*SELECT.*\{.*\}", RegexOptions.IgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.Security,
                    Severity = SafetySeverity.High,
                    Message = "Potential SQL injection vulnerability",
                    Recommendation = "Use parameterized queries or Entity Framework"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }

    /// <summary>
    /// Resource exhaustion safety rule
    /// </summary>
    public class ResourceExhaustionSafetyRule : SafetyRule
    {
        public override async Task<SafetyValidationResult> ValidateAsync(string code, CancellationToken cancellationToken)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for potential resource exhaustion
            if (Regex.IsMatch(code, @"while\s*\(\s*true\s*\)", RegexOptions.IgnoreCase))
            {
                result.Issues.Add(new SafetyIssue
                {
                    Type = SafetyIssueType.ResourceExhaustion,
                    Severity = SafetySeverity.Medium,
                    Message = "Infinite loop detected",
                    Recommendation = "Add proper exit conditions to prevent resource exhaustion"
                });
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }
}
