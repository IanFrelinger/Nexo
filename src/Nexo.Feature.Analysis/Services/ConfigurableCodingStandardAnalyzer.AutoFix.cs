using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Auto-fix functionality for ConfigurableCodingStandardAnalyzer.
    /// Handles automatic code fixing based on coding standards.
    /// </summary>
    public partial class ConfigurableCodingStandardAnalyzer
    {
        /// <summary>
        /// Automatically fixes code based on applicable standards.
        /// </summary>
        public async Task<(string FixedCode, List<string> AppliedFixes)> AutoFixCodeAsync(
            string code, 
            string? filePath = null, 
            string? agentId = null, 
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting auto-fix for code from {FilePath} by agent {AgentId}", filePath, agentId);

            var fixedCode = code;
            var appliedFixes = new List<string>();

            try
            {
                // Get applicable standards
                var applicableStandards = await GetApplicableStandardsAsync(filePath, agentId);

                foreach (var standard in applicableStandards)
                {
                    foreach (var rule in standard.Rules.Where(r => r.IsEnabled))
                    {
                        if (CanAutoFix(rule))
                        {
                            var (newCode, fixApplied) = ApplyAutoFix(fixedCode, rule);
                            if (fixApplied)
                            {
                                fixedCode = newCode;
                                appliedFixes.Add($"Applied fix for rule '{rule.Name}': {rule.SuggestedFix}");
                            }
                        }
                    }
                }

                _statistics.TotalAutoFixes += appliedFixes.Count;
                _logger.LogInformation("Auto-fix completed. Applied {FixCount} fixes", appliedFixes.Count);

                return (fixedCode, appliedFixes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during auto-fix");
                return (code, new List<string> { $"Auto-fix failed: {ex.Message}" });
            }
        }
    }
}
