using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Command to manage agent capabilities and focus areas (replaces Agent capability methods)
    /// </summary>
    public class ManageAgentCapabilitiesCommand : BaseCommand<ManageAgentCapabilitiesInput, ManageAgentCapabilitiesOutput>
    {
        public override string CommandId => "agent.manage-capabilities";
        public override string CommandName => "Manage Agent Capabilities";
        public override string Description => "Manages agent capabilities and focus areas (add, remove, list)";

        public override Task<CommandResult<ManageAgentCapabilitiesOutput>> ExecuteAsync(ManageAgentCapabilitiesInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var output = new ManageAgentCapabilitiesOutput
                {
                    AgentId = input.AgentId,
                    Action = input.Action,
                    Success = false
                };

                switch (input.Action)
                {
                    case CapabilityAction.AddFocusArea:
                        if (string.IsNullOrWhiteSpace(input.Item))
                        {
                            return Task.FromResult(CreateFailureResult("Focus area cannot be null or empty", startTime));
                        }
                        input.Agent.AddFocusArea(input.Item);
                        output.Success = true;
                        output.Message = $"Focus area '{input.Item}' added successfully";
                        break;

                    case CapabilityAction.RemoveFocusArea:
                        if (string.IsNullOrWhiteSpace(input.Item))
                        {
                            return Task.FromResult(CreateFailureResult("Focus area cannot be null or empty", startTime));
                        }
                        var focusRemoved = input.Agent.RemoveFocusArea(input.Item);
                        output.Success = focusRemoved;
                        output.Message = focusRemoved ? $"Focus area '{input.Item}' removed successfully" : $"Focus area '{input.Item}' not found";
                        break;

                    case CapabilityAction.AddCapability:
                        if (string.IsNullOrWhiteSpace(input.Item))
                        {
                            return Task.FromResult(CreateFailureResult("Capability cannot be null or empty", startTime));
                        }
                        input.Agent.AddCapability(input.Item);
                        output.Success = true;
                        output.Message = $"Capability '{input.Item}' added successfully";
                        break;

                    case CapabilityAction.RemoveCapability:
                        if (string.IsNullOrWhiteSpace(input.Item))
                        {
                            return Task.FromResult(CreateFailureResult("Capability cannot be null or empty", startTime));
                        }
                        var capabilityRemoved = input.Agent.RemoveCapability(input.Item);
                        output.Success = capabilityRemoved;
                        output.Message = capabilityRemoved ? $"Capability '{input.Item}' removed successfully" : $"Capability '{input.Item}' not found";
                        break;

                    case CapabilityAction.ListFocusAreas:
                        output.Success = true;
                        output.FocusAreas = input.Agent.FocusAreas.ToList();
                        output.Message = $"Found {output.FocusAreas.Count} focus areas";
                        break;

                    case CapabilityAction.ListCapabilities:
                        output.Success = true;
                        output.Capabilities = input.Agent.Capabilities.ToList();
                        output.Message = $"Found {output.Capabilities.Count} capabilities";
                        break;

                    default:
                        return Task.FromResult(CreateFailureResult($"Unknown capability action: {input.Action}", startTime));
                }

                return Task.FromResult(CreateSuccessResult(output, startTime));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to manage agent capabilities: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(ManageAgentCapabilitiesInput input)
        {
            return input != null && 
                   !string.IsNullOrWhiteSpace(input.AgentId) && 
                   input.Agent != null;
        }
    }
}
