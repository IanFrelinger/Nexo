using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Safety
{
    /// <summary>
    /// Enhanced safety validator that prevents harmful code generation
    /// </summary>
    public class EnhancedSafetyValidator
    {
        private readonly IModelProvider _aiProvider;
        private readonly ILogger<EnhancedSafetyValidator> _logger;
        private readonly List<SafetyRule> _safetyRules;

        public EnhancedSafetyValidator(IModelProvider aiProvider, ILogger<EnhancedSafetyValidator> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _safetyRules = InitializeSafetyRules();
        }

        /// <summary>
        /// Validates generated code for safety issues
        /// </summary>
        /// <param name="code">The code to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Safety validation result</returns>
        public async Task<SafetyValidationResult> ValidateGeneratedCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting enhanced safety validation");

                var result = new SafetyValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };

                // Apply all safety rules
                foreach (var rule in _safetyRules)
                {
                    var ruleResult = await rule.ValidateAsync(code, cancellationToken);
                    result.Issues.AddRange(ruleResult.Issues);
                    result.Recommendations.AddRange(ruleResult.Recommendations);
                }

                // AI-powered safety analysis
                var aiResult = await PerformAISafetyAnalysisAsync(code, cancellationToken);
                result.Issues.AddRange(aiResult.Issues);
                result.Recommendations.AddRange(aiResult.Recommendations);

                // Determine overall validity
                result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical || i.Severity == SafetySeverity.High);
                result.RequiresHumanReview = result.Issues.Any(i => i.Severity == SafetySeverity.High);
                result.IsBlocked = result.Issues.Any(i => i.Severity == SafetySeverity.Critical);

                _logger.LogInformation("Safety validation completed. Valid: {IsValid}, Issues: {IssueCount}, Blocked: {IsBlocked}",
                    result.IsValid, result.Issues.Count, result.IsBlocked);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during safety validation");
                throw;
            }
        }

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

        #region Private Methods

        private List<SafetyRule> InitializeSafetyRules()
        {
            return new List<SafetyRule>
            {
                new FileSystemSafetyRule(),
                new NetworkSafetyRule(),
                new SystemCommandSafetyRule(),
                new DataDestructionSafetyRule(),
                new SecurityVulnerabilitySafetyRule(),
                new ResourceExhaustionSafetyRule()
            };
        }

        private async Task<SafetyValidationResult> PerformAISafetyAnalysisAsync(string code, CancellationToken cancellationToken)
        {
            try
            {
                var prompt = $@"
Analyze the following C# code for safety issues and potential security vulnerabilities:

```csharp
{code}
```

Please identify:
1. File system operations that could be dangerous
2. Network operations that could be insecure
3. System command executions
4. Data destruction operations
5. Security vulnerabilities
6. Resource exhaustion risks

Rate each issue as Critical, High, Medium, or Low severity.
Provide specific recommendations for fixing each issue.
";

                var request = new ModelRequest
                {
                    Input = prompt,
                    Temperature = 0.1,
                    MaxTokens = 2000
                };

                var response = await _aiProvider.ExecuteAsync(request, cancellationToken);
                
                if (string.IsNullOrEmpty(response.Response))
                {
                    return new SafetyValidationResult
                    {
                        IsValid = true,
                        ValidationTime = DateTime.UtcNow,
                        Issues = new List<SafetyIssue>(),
                        Recommendations = new List<string>()
                    };
                }

                return ParseAISafetyResponse(response.Response);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI safety analysis failed, continuing with static analysis only");
                return new SafetyValidationResult
                {
                    IsValid = true,
                    ValidationTime = DateTime.UtcNow,
                    Issues = new List<SafetyIssue>(),
                    Recommendations = new List<string>()
                };
            }
        }

        private SafetyValidationResult ParseAISafetyResponse(string aiResponse)
        {
            var result = new SafetyValidationResult
            {
                IsValid = true,
                ValidationTime = DateTime.UtcNow,
                Issues = new List<SafetyIssue>(),
                Recommendations = new List<string>()
            };

            // Simple parsing of AI response - in production, this would be more sophisticated
            var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("-") || line.Trim().StartsWith("*"))
                {
                    var content = line.Trim().TrimStart('-', '*', ' ');
                    
                    // Try to extract severity
                    var severity = SafetySeverity.Low;
                    if (content.Contains("Critical", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.Critical;
                    else if (content.Contains("High", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.High;
                    else if (content.Contains("Medium", StringComparison.OrdinalIgnoreCase))
                        severity = SafetySeverity.Medium;

                    result.Issues.Add(new SafetyIssue
                    {
                        Type = SafetyIssueType.Security,
                        Severity = severity,
                        Message = content,
                        Recommendation = "Review and address the identified issue"
                    });
                }
            }

            result.IsValid = !result.Issues.Any(i => i.Severity == SafetySeverity.Critical || i.Severity == SafetySeverity.High);
            return result;
        }

        #endregion
    }

    /// <summary>
    /// Represents a safety validation result
    /// </summary>
    public class SafetyValidationResult
    {
        public bool IsValid { get; set; }
        public bool RequiresHumanReview { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime ValidationTime { get; set; }
        public List<SafetyIssue> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Represents a safety issue found in code
    /// </summary>
    public class SafetyIssue
    {
        public SafetyIssueType Type { get; set; }
        public SafetySeverity Severity { get; set; }
        public string Message { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public int LineNumber { get; set; }
        public string CodeSnippet { get; set; } = "";
    }

    /// <summary>
    /// Types of safety issues
    /// </summary>
    public enum SafetyIssueType
    {
        FileSystemAccess,
        NetworkAccess,
        SystemCommand,
        DataDestruction,
        Security,
        ResourceExhaustion
    }

    /// <summary>
    /// Severity levels for safety issues
    /// </summary>
    public enum SafetySeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
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
