using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Policy;

namespace Nexo.Infrastructure.Policy
{
    /// <summary>
    /// Safety policy functionality
    /// </summary>
    public partial class PolicyEngine
    {
        public async Task<SafetyPolicyResult> ApplySafetyPolicyAsync(string code, SafetyPolicy policy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying safety policy to code");

            var result = new SafetyPolicyResult { Passed = true, SafetyScore = 10.0 };

            try
            {
                // Check filesystem rules
                await CheckFilesystemRules(code, policy, result);

                // Check process rules
                await CheckProcessRules(code, policy, result);

                // Check network rules
                await CheckNetworkRules(code, policy, result);

                // Check content rules
                await CheckContentRules(code, policy, result);

                // Check license rules
                await CheckLicenseRules(code, policy, result);

                // Calculate final safety score
                result.SafetyScore = CalculateSafetyScore(result.Violations);
                result.Passed = result.Violations.All(v => v.Severity != "block" && v.Severity != "error");

                _logger.LogDebug("Safety policy applied. Passed: {Passed}, Score: {Score}", result.Passed, result.SafetyScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying safety policy");
                result.Passed = false;
                result.Violations.Add(new SafetyViolation
                {
                    RuleId = "safety-error",
                    Description = "Error applying safety policy",
                    Severity = "error",
                    Kind = "system",
                    Message = ex.Message
                });
            }

            return result;
        }

        private async Task CheckFilesystemRules(string code, SafetyPolicy policy, SafetyPolicyResult result)
        {
            await Task.CompletedTask;
            // Check for dangerous filesystem operations
            var dangerousPatterns = new[]
            {
                @"File\.Delete\s*\(\s*[""'][^""']*[""']\s*\)",
                @"Directory\.Delete\s*\(\s*[""'][^""']*[""']\s*\)",
                @"File\.Move\s*\(\s*[""'][^""']*[""']\s*,\s*[""'][^""']*[""']\s*\)"
            };

            foreach (var pattern in dangerousPatterns)
            {
                var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    result.Violations.Add(new SafetyViolation
                    {
                        RuleId = "fs-dangerous-operation",
                        Description = "Potentially dangerous filesystem operation",
                        Severity = "warn",
                        Kind = "filesystem",
                        Message = $"Dangerous filesystem operation: {match.Value}",
                        FixSuggestion = "Review filesystem operations for safety"
                    });
                }
            }
        }

        private async Task CheckProcessRules(string code, SafetyPolicy policy, SafetyPolicyResult result)
        {
            await Task.CompletedTask;
            // Check for dangerous process operations
            var dangerousPatterns = new[]
            {
                @"Process\.Start\s*\(\s*[""']cmd\.exe[""']",
                @"Process\.Start\s*\(\s*[""']powershell\.exe[""']",
                @"Process\.Start\s*\(\s*[""']bash[""']"
            };

            foreach (var pattern in dangerousPatterns)
            {
                var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    result.Violations.Add(new SafetyViolation
                    {
                        RuleId = "process-dangerous-exec",
                        Description = "Potentially dangerous process execution",
                        Severity = "error",
                        Kind = "process",
                        Message = $"Dangerous process execution: {match.Value}",
                        FixSuggestion = "Review process execution for safety"
                    });
                }
            }
        }

        private async Task CheckNetworkRules(string code, SafetyPolicy policy, SafetyPolicyResult result)
        {
            await Task.CompletedTask;
            // Check for insecure network operations
            var insecurePatterns = new[]
            {
                @"HttpClient.*http://",
                @"WebRequest\.Create\s*\(\s*[""']http://"
            };

            foreach (var pattern in insecurePatterns)
            {
                var matches = Regex.Matches(code, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    result.Violations.Add(new SafetyViolation
                    {
                        RuleId = "net-insecure-connection",
                        Description = "Insecure network connection detected",
                        Severity = "warn",
                        Kind = "network",
                        Message = $"Insecure connection: {match.Value}",
                        FixSuggestion = "Use HTTPS for secure connections"
                    });
                }
            }
        }

        private async Task CheckContentRules(string code, SafetyPolicy policy, SafetyPolicyResult result)
        {
            await Task.CompletedTask;
            // Check for secret patterns
            var secretPatterns = new[]
            {
                @"(?i)aws(.{0,20})?(secret|access)[=:]\s*([A-Za-z0-9/+=]{40})",
                @"(?i)api[_-]?key[=:]\s*[A-Za-z0-9\-_]{16,}"
            };

            foreach (var pattern in secretPatterns)
            {
                var matches = Regex.Matches(code, pattern);
                foreach (Match match in matches)
                {
                    result.Violations.Add(new SafetyViolation
                    {
                        RuleId = "content-secret-leak",
                        Description = "Potential secret leak detected",
                        Severity = "error",
                        Kind = "content",
                        Message = $"Potential secret: {match.Value}",
                        FixSuggestion = "Remove hardcoded secrets and use secure storage"
                    });
                }
            }
        }

        private async Task CheckLicenseRules(string code, SafetyPolicy policy, SafetyPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check for license compliance
            // For now, we'll just log that it's not implemented
            _logger.LogDebug("License compliance checking not implemented");
        }

        private double CalculateSafetyScore(List<SafetyViolation> violations)
        {
            if (!violations.Any()) return 10.0;

            var totalPenalty = violations.Sum(v => v.Severity switch
            {
                "block" => 3.0,
                "error" => 2.0,
                "warn" => 1.0,
                "info" => 0.5,
                _ => 0.0
            });

            return Math.Max(0.0, 10.0 - totalPenalty);
        }
    }
}
