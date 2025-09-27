using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Manages agent capability expansion and learning
    /// This class acts as an orchestrator, delegating specific functionalities to partial class implementations.
    /// </summary>
    public partial class AgentCapabilityExpansion : IAgentCapabilityExpansion
    {
        private readonly ILogger<AgentCapabilityExpansion> _logger;
        private readonly IAgentLearningSystem _learningSystem;
        private readonly IAgentCapabilityRegistry _capabilityRegistry;
        private readonly IUserFeedbackProcessor _feedbackProcessor;
        private readonly IExpansionStrategySelector _strategySelector;

        public AgentCapabilityExpansion(
            ILogger<AgentCapabilityExpansion> logger,
            IAgentLearningSystem learningSystem,
            IAgentCapabilityRegistry capabilityRegistry,
            IUserFeedbackProcessor feedbackProcessor,
            IExpansionStrategySelector strategySelector)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _learningSystem = learningSystem ?? throw new ArgumentNullException(nameof(learningSystem));
            _capabilityRegistry = capabilityRegistry ?? throw new ArgumentNullException(nameof(capabilityRegistry));
            _feedbackProcessor = feedbackProcessor ?? throw new ArgumentNullException(nameof(feedbackProcessor));
            _strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        }
        // This class acts as an orchestrator for various agent capability expansion functionalities,
        // with specific categories defined in partial classes.
    }
}
