using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services
{
    /// <summary>
    /// Standards management functionality for ConfigurableCodingStandardAnalyzer.
    /// Handles standards retrieval and filtering based on agents and file types.
    /// </summary>
    public partial class ConfigurableCodingStandardAnalyzer
    {
        /// <summary>
        /// Gets all available standards.
        /// </summary>
        public async Task<List<CodingStandard>> GetAvailableStandardsAsync()
        {
            return await Task.FromResult(_configuration.Standards.ToList());
        }

        /// <summary>
        /// Gets standards applicable to a specific agent.
        /// </summary>
        public async Task<List<CodingStandard>> GetStandardsForAgentAsync(string agentId)
        {
            var agentSettings = _configuration.AgentSettings.GetValueOrDefault(agentId);
            if (agentSettings == null || !agentSettings.IsEnabled)
            {
                return new List<CodingStandard>();
            }

            var applicableStandards = _configuration.Standards
                .Where(s => s.IsEnabled && (agentSettings.AppliedStandards.Count == 0 || agentSettings.AppliedStandards.Contains(s.Id)))
                .Where(s => !agentSettings.ExcludedRules.Any(er => s.Rules.Any(r => r.Id == er)))
                .ToList();

            return await Task.FromResult(applicableStandards);
        }

        /// <summary>
        /// Gets standards applicable to a specific file type.
        /// </summary>
        public async Task<List<CodingStandard>> GetStandardsForFileTypeAsync(string fileExtension)
        {
            var fileTypeSettings = _configuration.FileTypeSettings.GetValueOrDefault(fileExtension);
            if (fileTypeSettings == null || !fileTypeSettings.IsEnabled)
            {
                return new List<CodingStandard>();
            }

            var applicableStandards = _configuration.Standards
                .Where(s => s.IsEnabled && (fileTypeSettings.AppliedStandards.Count == 0 || fileTypeSettings.AppliedStandards.Contains(s.Id)))
                .Where(s => !fileTypeSettings.ExcludedRules.Any(er => s.Rules.Any(r => r.Id == er)))
                .ToList();

            return await Task.FromResult(applicableStandards);
        }
    }
}
