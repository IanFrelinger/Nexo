using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Command to create a new agent (replaces Agent constructor and initialization)
    /// </summary>
    public class CreateAgentCommand : BaseCommand<CreateAgentInput, CreateAgentOutput>
    {
        public override string CommandId => "agent.create";
        public override string CommandName => "Create Agent";
        public override string Description => "Creates a new agent with the specified properties and initializes it";

        public override Task<CommandResult<CreateAgentOutput>> ExecuteAsync(CreateAgentInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(input.Name))
                {
                    return Task.FromResult(CreateFailureResult("Agent name cannot be null or empty", startTime));
                }

                // Create agent ID and name value objects
                var agentId = new AgentId(Guid.NewGuid().ToString());
                var agentName = new AgentName(input.Name);
                var agentRole = input.Role ?? AgentRole.Developer;

                // Create the agent entity
                var agent = new Nexo.Core.Domain.Entities.Agent(agentId, agentName, agentRole);

                // Set initial focus areas if provided
                if (input.FocusAreas != null)
                {
                    foreach (var focusArea in input.FocusAreas)
                    {
                        agent.AddFocusArea(focusArea);
                    }
                }

                // Set initial capabilities if provided
                if (input.Capabilities != null)
                {
                    foreach (var capability in input.Capabilities)
                    {
                        agent.AddCapability(capability);
                    }
                }

                // Set initial configuration if provided
                if (input.Configuration != null)
                {
                    foreach (var kvp in input.Configuration)
                    {
                        agent.SetConfiguration(kvp.Key, kvp.Value);
                    }
                }

                var output = new CreateAgentOutput
                {
                    Agent = agent,
                    AgentId = agentId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                var warnings = new List<string>();
                if (input.FocusAreas == null || input.FocusAreas.Count == 0)
                {
                    warnings.Add("No initial focus areas provided for the agent");
                }

                if (input.Capabilities == null || input.Capabilities.Count == 0)
                {
                    warnings.Add("No initial capabilities provided for the agent");
                }

                return Task.FromResult(CreateSuccessResult(output, startTime, warnings.ToArray()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to create agent: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(CreateAgentInput input)
        {
            return input != null && !string.IsNullOrWhiteSpace(input.Name);
        }
    }
}
