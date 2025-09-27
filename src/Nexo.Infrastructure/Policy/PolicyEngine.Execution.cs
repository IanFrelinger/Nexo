using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Models.Policy;
using YamlDotNet.Serialization;

namespace Nexo.Infrastructure.Policy
{
    /// <summary>
    /// Policy execution functionality
    /// </summary>
    public partial class PolicyEngine
    {
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
    }
}
