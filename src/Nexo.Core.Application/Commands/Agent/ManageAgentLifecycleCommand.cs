using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Commands.Agent
{
    /// <summary>
    /// Command to manage agent lifecycle (replaces Agent lifecycle methods)
    /// </summary>
    public class ManageAgentLifecycleCommand : BaseCommand<ManageAgentLifecycleInput, ManageAgentLifecycleOutput>
    {
        public override string CommandId => "agent.manage-lifecycle";
        public override string CommandName => "Manage Agent Lifecycle";
        public override string Description => "Manages agent lifecycle operations like activate, deactivate, set busy, set failed";

        public override Task<CommandResult<ManageAgentLifecycleOutput>> ExecuteAsync(ManageAgentLifecycleInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var output = new ManageAgentLifecycleOutput
                {
                    AgentId = input.AgentId,
                    Action = input.Action,
                    Success = false
                };

                switch (input.Action)
                {
                    case AgentLifecycleAction.Activate:
                        input.Agent.Activate();
                        output.Success = true;
                        output.Message = "Agent activated successfully";
                        break;

                    case AgentLifecycleAction.Deactivate:
                        input.Agent.Deactivate();
                        output.Success = true;
                        output.Message = "Agent deactivated successfully";
                        break;

                    case AgentLifecycleAction.SetBusy:
                        input.Agent.SetBusy();
                        output.Success = true;
                        output.Message = "Agent set to busy status";
                        break;

                    case AgentLifecycleAction.SetFailed:
                        if (string.IsNullOrWhiteSpace(input.FailureReason))
                        {
                            return Task.FromResult(CreateFailureResult("Failure reason is required for SetFailed action", startTime));
                        }
                        input.Agent.SetFailed(input.FailureReason);
                        output.Success = true;
                        output.Message = $"Agent set to failed status: {input.FailureReason}";
                        break;

                    default:
                        return Task.FromResult(CreateFailureResult($"Unknown lifecycle action: {input.Action}", startTime));
                }

                output.NewStatus = input.Agent.Status;
                return Task.FromResult(CreateSuccessResult(output, startTime));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to manage agent lifecycle: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(ManageAgentLifecycleInput input)
        {
            return input != null && 
                   !string.IsNullOrWhiteSpace(input.AgentId) && 
                   input.Agent != null;
        }
    }
}
