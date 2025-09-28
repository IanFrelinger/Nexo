using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Factory method to create a default agent
    /// </summary>
    public static class AgentFactory
    {
        public static Nexo.Core.Domain.Entities.Agent CreateDefaultAgent()
        {
            var id = new AgentId(Guid.NewGuid().ToString());
            var name = new AgentName("Default Agent");
            var role = AgentRole.Developer;
            return new Nexo.Core.Domain.Entities.Agent(id, name, role);
        }
    }
}
