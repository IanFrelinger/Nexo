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
    /// Adaptation functionality
    /// </summary>
    public partial class KnowledgeBase
    {
        /// <summary>
        /// Updates the adaptation state with new context and adaptations.
        /// </summary>
        public async Task UpdateAdaptationStateAsync(
            EnvironmentContext context,
            List<AdaptationAction> adaptations,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Updating adaptation state for environment {EnvironmentType}", 
                context.EnvironmentType);

            try
            {
                var insight = new LearningInsight
                {
                    Type = "AdaptationState",
                    Description = $"Environment adaptation: {context.EnvironmentType}",
                    ConfidenceLevel = 90.0,
                    Data = new Dictionary<string, object>
                    {
                        { "environmentType", context.EnvironmentType.ToString() },
                        { "environmentName", context.EnvironmentName },
                        { "adaptationCount", adaptations.Count },
                        { "adaptations", adaptations.Select(a => new { a.Type, a.Description, a.Priority }) }
                    },
                    Source = "AdaptationEngine"
                };

                await StoreInsightAsync(insight, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating adaptation state");
                throw;
            }
        }
    }
}
