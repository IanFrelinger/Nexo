using System;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Output for creating an agent
    /// </summary>
    public class CreateAgentOutput
    {
        public Nexo.Core.Domain.Entities.Agent Agent { get; set; } = AgentFactory.CreateDefaultAgent();
        public string AgentId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
