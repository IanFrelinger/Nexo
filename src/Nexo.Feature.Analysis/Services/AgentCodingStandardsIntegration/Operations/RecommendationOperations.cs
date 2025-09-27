using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;

namespace Nexo.Feature.Analysis.Services.AgentCodingStandardsIntegration.Operations
{
    public class RecommendationOperations
    {
        private readonly ICodingStandardAnalyzer _analyzer;
        private readonly ILogger _logger;

        public RecommendationOperations(ICodingStandardAnalyzer analyzer, ILogger logger)
        {
            _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<CodingStandardRecommendation>> GetAgentRecommendationsAsync(string agentId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Getting coding standards recommendations for agent {AgentId}", agentId);
            try
            {
                var applicableStandards = await _analyzer.GetStandardsForAgentAsync(agentId);
                var recommendations = new List<CodingStandardRecommendation>();
                foreach (var standard in applicableStandards)
                {
                    recommendations.Add(new CodingStandardRecommendation
                    {
                        AgentId = agentId,
                        StandardId = standard.Id,
                        StandardName = standard.Name,
                        Description = standard.Description,
                        Priority = standard.Priority,
                        Rules = standard.Rules.Where(r => r.IsEnabled).ToList(),
                        EstimatedImpact = CalculateEstimatedImpact(standard),
                        ImplementationGuidance = GenerateImplementationGuidance(standard)
                    });
                }
                return recommendations.OrderByDescending(r => r.Priority).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for agent {AgentId}", agentId);
                return new List<CodingStandardRecommendation>();
            }
        }

        private static string CalculateEstimatedImpact(CodingStandard standard)
        {
            var ruleCount = standard.Rules.Count(r => r.IsEnabled);
            var criticalRules = standard.Rules.Count(r => r.IsEnabled && r.Severity == CodingStandardSeverity.Critical);
            var errorRules = standard.Rules.Count(r => r.IsEnabled && r.Severity == CodingStandardSeverity.Error);
            if (criticalRules > 0) return "High - Contains critical rules that must be followed";
            if (errorRules > 2) return "Medium-High - Contains multiple error-level rules";
            if (ruleCount > 5) return "Medium - Contains many rules to follow";
            return "Low - Minimal impact on code generation";
        }

        private static string GenerateImplementationGuidance(CodingStandard standard)
        {
            var guidance = new List<string>
            {
                $"Apply {standard.Name} to {standard.Language} code generation",
                $"Focus on {string.Join(", ", standard.Rules.Where(r => r.IsEnabled).Select(r => r.Category).Distinct())} categories"
            };
            if (standard.Rules.Any(r => r.IsEnabled && r.Severity == CodingStandardSeverity.Critical))
                guidance.Add("Pay special attention to critical severity rules");
            if (standard.Rules.Any(r => r.IsEnabled && r.Type == CodingStandardRuleType.Security))
                guidance.Add("Ensure security rules are strictly followed");
            return string.Join(". ", guidance) + ".";
        }
    }
}


