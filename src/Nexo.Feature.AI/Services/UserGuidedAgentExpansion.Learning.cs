using System;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.AI.Services
{
    /// <summary>
    /// Learning from expansion and rollback functionality
    /// </summary>
    public partial class UserGuidedAgentExpansion
    {
        private async Task LearnFromExpansionAsync(AgentExpansionRequest request, ExpansionResult result)
        {
            // Learn from the expansion process to improve future expansions
            await _learningSystem.RecordExpansionExperienceAsync(request, result);
        }
    }
}
