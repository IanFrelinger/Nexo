using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Policy;

namespace Nexo.Infrastructure.Policy
{
    /// <summary>
    /// Core policy engine functionality
    /// </summary>
    public partial class PolicyEngine
    {
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
    }
}
