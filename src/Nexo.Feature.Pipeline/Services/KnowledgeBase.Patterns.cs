using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using ExecutionContext = Nexo.Feature.Pipeline.Models.ExecutionContext;

namespace Nexo.Feature.Pipeline.Services
{
    /// <summary>
    /// Pattern storage functionality
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Stores a bottleneck pattern for future analysis.
        /// </summary>
        public async Task StoreBottleneckPatternAsync(
            PerformanceBottleneck bottleneck,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing bottleneck pattern: {Type} - {Description}", 
                bottleneck.Type, bottleneck.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "Bottleneck",
                    Description = $"Performance bottleneck: {bottleneck.Description}",
                    ConfidenceLevel = 90.0,
                    Data = new Dictionary<string, object>
                    {
                        { "bottleneckType", bottleneck.Type.ToString() },
                        { "severity", bottleneck.Severity.ToString() },
                        { "impactPercentage", bottleneck.ImpactPercentage },
                        { "affectedComponent", bottleneck.AffectedComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing bottleneck pattern");
                throw;
            }
        }

        /// <summary>
        /// Stores a strength pattern for future analysis.
        /// </summary>
        public async Task StoreStrengthPatternAsync(
            PerformanceStrength strength,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing strength pattern: {Type} - {Description}", 
                strength.Type, strength.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "Strength",
                    Description = $"Performance strength: {strength.Description}",
                    ConfidenceLevel = 85.0,
                    Data = new Dictionary<string, object>
                    {
                        { "strengthType", strength.Type.ToString() },
                        { "benefitPercentage", strength.BenefitPercentage },
                        { "sourceComponent", strength.SourceComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing strength pattern");
                throw;
            }
        }

        /// <summary>
        /// Stores an optimization opportunity for future analysis.
        /// </summary>
        public async Task StoreOptimizationOpportunityAsync(
            OptimizationOpportunity opportunity,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Storing optimization opportunity: {Type} - {Description}", 
                opportunity.Type, opportunity.Description);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "OptimizationOpportunity",
                    Description = $"Optimization opportunity: {opportunity.Description}",
                    ConfidenceLevel = 80.0,
                    Data = new Dictionary<string, object>
                    {
                        { "opportunityType", opportunity.Type.ToString() },
                        { "potentialImprovement", opportunity.PotentialImprovementPercentage },
                        { "implementationComplexity", opportunity.ImplementationComplexity.ToString() },
                        { "targetComponent", opportunity.TargetComponent }
                    },
                    Source = "PerformanceAnalysis"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing optimization opportunity");
                throw;
            }
        }
    }
}
