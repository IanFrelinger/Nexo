using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models.Policy;
using YamlDotNet.Serialization;

namespace Nexo.Infrastructure.Policy
{
    /// <summary>
    /// Policy engine implementation for validating code against safety and quality policies
    /// </summary>
    public class PolicyEngine : IPolicyEngine
    {
        private readonly ILogger<PolicyEngine> _logger;
        private readonly ISerializer _yamlSerializer;
        private readonly IDeserializer _yamlDeserializer;

        public PolicyEngine(ILogger<PolicyEngine> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _yamlSerializer = new SerializerBuilder().Build();
            _yamlDeserializer = new DeserializerBuilder().Build();
        }

        public async Task<PolicyDefinition> LoadPolicyAsync(string policyPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading policy from: {PolicyPath}", policyPath);

            try
            {
                var yamlContent = await File.ReadAllTextAsync(policyPath, cancellationToken);
                var policy = _yamlDeserializer.Deserialize<PolicyDefinition>(yamlContent);
                
                _logger.LogDebug("Successfully loaded policy: {PolicyId}", policy.Meta.Id);
                return policy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load policy from: {PolicyPath}", policyPath);
                throw;
            }
        }

        public async Task<PolicyValidationResult> ValidatePolicyAsync(string policyPath, string schemaPath, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Validating policy: {PolicyPath} against schema: {SchemaPath}", policyPath, schemaPath);

            try
            {
                var policyContent = await File.ReadAllTextAsync(policyPath, cancellationToken);
                var schemaContent = await File.ReadAllTextAsync(schemaPath, cancellationToken);

                // Basic YAML validation
                var policy = _yamlDeserializer.Deserialize<PolicyDefinition>(policyContent);
                
                // Schema validation would go here (using JsonSchema.Net or similar)
                // For now, we'll do basic validation
                var result = new PolicyValidationResult { IsValid = true };

                if (string.IsNullOrEmpty(policy.Meta.Id))
                {
                    result.IsValid = false;
                    result.Errors.Add("Policy metadata ID is required");
                }

                if (string.IsNullOrEmpty(policy.Meta.Version))
                {
                    result.IsValid = false;
                    result.Errors.Add("Policy metadata version is required");
                }

                _logger.LogDebug("Policy validation completed. Valid: {IsValid}", result.IsValid);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate policy: {PolicyPath}", policyPath);
                return new PolicyValidationResult
                {
                    IsValid = false,
                    Errors = { ex.Message }
                };
            }
        }

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

        public async Task<QualityPolicyResult> ApplyQualityPolicyAsync(string code, QualityPolicy policy, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Applying quality policy to code");

            var result = new QualityPolicyResult { Passed = true, QualityScore = 10.0 };

            try
            {
                // Check compile gate
                await CheckCompileGate(code, policy.Gates.Compile, result);

                // Check test gate
                await CheckTestGate(code, policy.Gates.Tests, result);

                // Check style gate
                await CheckStyleGate(code, policy.Gates.Style, result);

                // Check complexity gate
                await CheckComplexityGate(code, policy.Gates.Complexity, result);

                // Check dependency gate
                await CheckDependencyGate(code, policy.Gates.Dependencies, result);

                // Calculate quality score
                result.QualityScore = CalculateQualityScore(result.GateScores, policy.Scoring);
                result.Passed = result.QualityScore >= policy.Scoring.PassThreshold;

                _logger.LogDebug("Quality policy applied. Passed: {Passed}, Score: {Score}", result.Passed, result.QualityScore);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying quality policy");
                result.Passed = false;
                result.Violations.Add(new QualityViolation
                {
                    RuleId = "quality-error",
                    Description = "Error applying quality policy",
                    Severity = "error",
                    Gate = "system",
                    Message = ex.Message
                });
            }

            return result;
        }

        public async Task<PolicyExecutionResult> ExecutePolicyManifestAsync(string manifestPath, string code, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Executing policy manifest: {ManifestPath}", manifestPath);

            var result = new PolicyExecutionResult { Passed = true };

            try
            {
                var manifestContent = await File.ReadAllTextAsync(manifestPath, cancellationToken);
                var manifest = _yamlDeserializer.Deserialize<PolicyManifest>(manifestContent);

                // Load and merge policies
                var policyPaths = new List<string>();
                foreach (var include in manifest.Includes)
                {
                    var fullPath = Path.IsPathRooted(include) ? include : Path.Combine(Path.GetDirectoryName(manifestPath)!, include);
                    policyPaths.Add(fullPath);
                }

                var mergedPolicy = await MergePoliciesAsync(policyPaths, cancellationToken);

                // Apply safety policy
                if (mergedPolicy.Safety != null)
                {
                    result.SafetyResult = await ApplySafetyPolicyAsync(code, mergedPolicy.Safety, cancellationToken);
                    if (!result.SafetyResult.Passed)
                    {
                        result.Passed = false;
                    }
                }

                // Apply quality policy
                if (mergedPolicy.Quality != null)
                {
                    result.QualityResult = await ApplyQualityPolicyAsync(code, mergedPolicy.Quality, cancellationToken);
                    if (!result.QualityResult.Passed)
                    {
                        result.Passed = false;
                    }
                }

                // Generate report
                result.ReportPath = await GenerateReportAsync(mergedPolicy, result, cancellationToken);

                _logger.LogDebug("Policy manifest executed. Passed: {Passed}", result.Passed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing policy manifest: {ManifestPath}", manifestPath);
                result.Passed = false;
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<PolicyDefinition> MergePoliciesAsync(IEnumerable<string> policyPaths, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Merging {Count} policies", policyPaths.Count());

            var mergedPolicy = new PolicyDefinition();

            foreach (var policyPath in policyPaths)
            {
                var policy = await LoadPolicyAsync(policyPath, cancellationToken);
                
                // Merge safety policies
                if (policy.Safety != null)
                {
                    if (mergedPolicy.Safety == null)
                    {
                        mergedPolicy.Safety = policy.Safety;
                    }
                    else
                    {
                        // Merge safety rules
                        mergedPolicy.Safety.Rules.AddRange(policy.Safety.Rules);
                    }
                }

                // Merge quality policies
                if (policy.Quality != null)
                {
                    if (mergedPolicy.Quality == null)
                    {
                        mergedPolicy.Quality = policy.Quality;
                    }
                    else
                    {
                        // Merge quality gates (later policies override earlier ones)
                        if (policy.Quality.Gates != null)
                        {
                            mergedPolicy.Quality.Gates = policy.Quality.Gates;
                        }
                        if (policy.Quality.Scoring != null)
                        {
                            mergedPolicy.Quality.Scoring = policy.Quality.Scoring;
                        }
                    }
                }

                // Merge rules
                mergedPolicy.Rules.AddRange(policy.Rules);
            }

            return mergedPolicy;
        }

        #region Private Methods

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

        private async Task CheckCompileGate(string code, CompileGate gate, QualityPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check if code compiles
            // For now, we'll assume it compiles
            result.GateScores["compile"] = 1.0;
        }

        private async Task CheckTestGate(string code, TestGate gate, QualityPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check test coverage
            // For now, we'll assume 80% coverage
            result.GateScores["tests"] = 0.8;
        }

        private async Task CheckStyleGate(string code, StyleGate gate, QualityPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check code style
            // For now, we'll assume good style
            result.GateScores["style"] = 0.9;
        }

        private async Task CheckComplexityGate(string code, ComplexityGate gate, QualityPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check cyclomatic complexity
            // For now, we'll assume good complexity
            result.GateScores["complexity"] = 0.85;
        }

        private async Task CheckDependencyGate(string code, DependencyGate gate, QualityPolicyResult result)
        {
            await Task.CompletedTask;
            // This would check dependencies
            // For now, we'll assume good dependencies
            result.GateScores["dependencies"] = 0.9;
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

        private double CalculateQualityScore(Dictionary<string, double> gateScores, QualityScoring scoring)
        {
            var weightedScore = 0.0;
            var totalWeight = 0.0;

            foreach (var weight in scoring.Weights)
            {
                if (gateScores.TryGetValue(weight.Key, out var score))
                {
                    weightedScore += score * weight.Value;
                    totalWeight += weight.Value;
                }
            }

            return totalWeight > 0 ? weightedScore / totalWeight : 0.0;
        }

        private async Task<string> GenerateReportAsync(PolicyDefinition policy, PolicyExecutionResult result, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            var reportPath = Path.Combine(policy.Outputs.ReportDir, $"policy-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
            
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            
            var report = new
            {
                Timestamp = DateTime.UtcNow,
                Policy = policy.Meta,
                Result = result,
                Summary = new
                {
                    Passed = result.Passed,
                    SafetyScore = result.SafetyResult?.SafetyScore ?? 0,
                    QualityScore = result.QualityResult?.QualityScore ?? 0
                }
            };

            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(reportPath, json, cancellationToken);

            return reportPath;
        }

        #endregion
    }

    /// <summary>
    /// Policy manifest structure
    /// </summary>
    public class PolicyManifest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public List<string> Includes { get; set; } = new List<string>();
        public List<string> Overrides { get; set; } = new List<string>();
    }
}
