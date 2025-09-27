using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Coordination;
using Nexo.Feature.AI.Interfaces;

namespace Nexo.Feature.AI.Services.EnhancedFeatureFactory.Execution
{
    public class CoordinationExecutor
    {
        private readonly IAgentCoordinator _agentCoordinator;
        private readonly ISpecializedAgentRegistry _agentRegistry;
        private readonly ILogger _logger;

        public CoordinationExecutor(IAgentCoordinator agentCoordinator, ISpecializedAgentRegistry agentRegistry, ILogger logger)
        {
            _agentCoordinator = agentCoordinator ?? throw new ArgumentNullException(nameof(agentCoordinator));
            _agentRegistry = agentRegistry ?? throw new ArgumentNullException(nameof(agentRegistry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ComplexAgentRequest BuildComplexAgentRequest(FeatureGenerationRequest request, RequestComplexityAnalysis analysis)
        {
            return new ComplexAgentRequest
            {
                Description = request.Description,
                TargetPlatforms = request.TargetPlatforms,
                PerformanceRequirements = request.PerformanceRequirements,
                SecurityRequirements = request.SecurityRequirements,
                QualityRequirements = request.QualityRequirements,
                Context = new Dictionary<string, object>
                {
                    ["ComplexityLevel"] = analysis.ComplexityLevel,
                    ["RequiredSpecializations"] = analysis.RequiredSpecializations,
                    ["EstimatedEffort"] = analysis.EstimatedEffort
                }
            };
        }

        public Task<CoordinatedResponse> ExecuteAsync(ComplexAgentRequest complexRequest)
        {
            return _agentCoordinator.CoordinateComplexTaskAsync(complexRequest);
        }
    }
}


