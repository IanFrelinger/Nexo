using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Safety
{
    /// <summary>
    /// Specific validation methods for EnhancedSafetyValidator.
    /// </summary>
    public partial class EnhancedSafetyValidator
    {
        /// <summary>
        /// Validates code for file system access safety
        /// </summary>
        public async Task<SafetyValidationResult> ValidateFileSystemAccessAsync(string code, CancellationToken cancellationToken = default)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for dangerous file operations
            var dangerousPatterns = new[]
            {
                (@"File\.Delete\s*\(\s*[""'][^""']*[""']", "File deletion without validation"),
                (@"Directory\.Delete\s*\(\s*[""'][^""']*[""']", "Directory deletion without validation"),
                (@"File\.Move\s*\(\s*[""'][^""']*[""']", "File move without validation"),
                (@"File\.Copy\s*\(\s*[""'][^""']*[""']", "File copy without validation"),
                (@"File\.WriteAllText\s*\(\s*[""'][^""']*[""']", "File write without validation"),
                (@"File\.WriteAllBytes\s*\(\s*[""'][^""']*[""']", "File write without validation")
            };

            foreach (var (pattern, description) in dangerousPatterns)
            {
                if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.FileSystemAccess,
                        Severity = SafetySeverity.Critical,
                        Message = description,
                        Recommendation = "Validate file paths and implement proper authorization"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Validates code for network access safety
        /// </summary>
        public async Task<SafetyValidationResult> ValidateNetworkAccessAsync(string code, CancellationToken cancellationToken = default)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for insecure network operations
            var insecurePatterns = new[]
            {
                (@"HttpClient.*http://", "Insecure HTTP connection"),
                (@"WebRequest.*http://", "Insecure HTTP connection"),
                (@"Socket.*Connect", "Direct socket connection without validation"),
                (@"TcpClient.*Connect", "Direct TCP connection without validation")
            };

            foreach (var (pattern, description) in insecurePatterns)
            {
                if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.NetworkAccess,
                        Severity = SafetySeverity.Medium,
                        Message = description,
                        Recommendation = "Use HTTPS and validate network endpoints"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Validates code for system command execution safety
        /// </summary>
        public async Task<SafetyValidationResult> ValidateSystemCommandsAsync(string code, CancellationToken cancellationToken = default)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for dangerous system commands
            var dangerousCommands = new[]
            {
                "Process.Start",
                "System.Diagnostics.Process",
                "cmd.exe",
                "powershell.exe",
                "bash",
                "sh",
                "rm -rf",
                "del /f",
                "format",
                "shutdown",
                "reboot"
            };

            foreach (var command in dangerousCommands)
            {
                if (code.Contains(command, StringComparison.OrdinalIgnoreCase))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.SystemCommand,
                        Severity = SafetySeverity.Critical,
                        Message = $"Dangerous system command detected: {command}",
                        Recommendation = "Avoid executing system commands or implement proper sandboxing"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }

        /// <summary>
        /// Validates code for data destruction safety
        /// </summary>
        public async Task<SafetyValidationResult> ValidateDataDestructionAsync(string code, CancellationToken cancellationToken = default)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Check for data destruction patterns
            var destructionPatterns = new[]
            {
                (@"DELETE\s+FROM\s+\w+", "SQL DELETE without WHERE clause"),
                (@"TRUNCATE\s+TABLE", "SQL TRUNCATE without validation"),
                (@"DROP\s+TABLE", "SQL DROP TABLE without validation"),
                (@"File\.Delete.*\*", "File deletion with wildcard"),
                (@"Directory\.Delete.*\*", "Directory deletion with wildcard")
            };

            foreach (var (pattern, description) in destructionPatterns)
            {
                if (Regex.IsMatch(code, pattern, RegexOptions.IgnoreCase))
                {
                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.DataDestruction,
                        Severity = SafetySeverity.Critical,
                        Message = description,
                        Recommendation = "Implement proper data validation and backup mechanisms"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical);
            return await Task.FromResult(result);
        }
    }
}
