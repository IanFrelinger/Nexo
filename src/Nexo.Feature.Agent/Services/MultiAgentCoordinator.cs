using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Feature.Agent.Services
{
    /// <summary>
    /// Multi-agent coordinator responsible for managing agent interactions, collaboration sessions,
    /// and facilitating agent-to-agent communication within the system.
    /// This class is split into partial files for better organization:
    /// - MultiAgentCoordinator.Core.cs - Agent registration and basic management
    /// - MultiAgentCoordinator.Collaboration.cs - Collaboration session management
    /// - MultiAgentCoordinator.Tasks.cs - Collaborative task execution
    /// - MultiAgentCoordinator.Communication.cs - Agent-to-agent communication
    /// - MultiAgentCoordinator.Analysis.cs - Collaboration pattern analysis
    /// - MultiAgentCoordinator.Selection.cs - Agent selection for collaboration
    /// - MultiAgentCoordinator.Synthesis.cs - Result synthesis and coordination
    /// </summary>
    public partial class MultiAgentCoordinator : IMultiAgentCoordinator
    {
        // This class is split into partial files for better organization
        // All functionality has been moved to the respective partial files listed above
    }
}
