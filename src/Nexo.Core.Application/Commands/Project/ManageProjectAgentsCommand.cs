using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.ValueObjects;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Command to manage project agents (replaces Project agent management methods)
    /// </summary>
    public class ManageProjectAgentsCommand : BaseCommand<ManageProjectAgentsInput, ManageProjectAgentsOutput>
    {
        public override string CommandId => "project.manage-agents";
        public override string CommandName => "Manage Project Agents";
        public override string Description => "Adds, removes, or retrieves agents from a project";

        public override Task<CommandResult<ManageProjectAgentsOutput>> ExecuteAsync(ManageProjectAgentsInput input, CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;

            try
            {
                var output = new ManageProjectAgentsOutput
                {
                    ProjectId = input.ProjectId,
                    Action = input.Action,
                    Agents = new List<Nexo.Core.Domain.Entities.Agent>(),
                    Success = false
                };

                switch (input.Action)
                {
                    case AgentAction.Add:
                        output = HandleAddAgent(input, output);
                        break;
                    case AgentAction.Remove:
                        output = HandleRemoveAgent(input, output);
                        break;
                    case AgentAction.Get:
                        output = HandleGetAgent(input, output);
                        break;
                    case AgentAction.List:
                        output = HandleListAgents(input, output);
                        break;
                    default:
                        return Task.FromResult(CreateFailureResult($"Unknown agent action: {input.Action}", startTime));
                }

                return Task.FromResult(CreateSuccessResult(output, startTime));
            }
            catch (Exception ex)
            {
                return Task.FromResult(CreateFailureResult($"Failed to manage project agents: {ex.Message}", startTime));
            }
        }

        protected override bool ValidateInput(ManageProjectAgentsInput input)
        {
            return input != null && 
                   !string.IsNullOrWhiteSpace(input.ProjectId) && 
                   input.Project != null;
        }

        private ManageProjectAgentsOutput HandleAddAgent(ManageProjectAgentsInput input, ManageProjectAgentsOutput output)
        {
            if (input.Agent == null)
            {
                output.ErrorMessage = "Agent cannot be null for add action";
                return output;
            }

            try
            {
                input.Project.AddAgent(input.Agent);
                output.Success = true;
                output.Agents = input.Project.Agents.ToList();
                output.Message = $"Agent {input.Agent.Id} added successfully";
            }
            catch (InvalidOperationException ex)
            {
                output.ErrorMessage = ex.Message;
            }

            return output;
        }

        private ManageProjectAgentsOutput HandleRemoveAgent(ManageProjectAgentsInput input, ManageProjectAgentsOutput output)
        {
            if (string.IsNullOrWhiteSpace(input.AgentId))
            {
                output.ErrorMessage = "Agent ID cannot be null or empty for remove action";
                return output;
            }

            try
            {
                var agentId = new AgentId(input.AgentId);
                var removed = input.Project.RemoveAgent(agentId);
                output.Success = removed;
                output.Agents = input.Project.Agents.ToList();
                output.Message = removed ? $"Agent {input.AgentId} removed successfully" : $"Agent {input.AgentId} not found";
            }
            catch (Exception ex)
            {
                output.ErrorMessage = ex.Message;
            }

            return output;
        }

        private ManageProjectAgentsOutput HandleGetAgent(ManageProjectAgentsInput input, ManageProjectAgentsOutput output)
        {
            if (string.IsNullOrWhiteSpace(input.AgentId))
            {
                output.ErrorMessage = "Agent ID cannot be null or empty for get action";
                return output;
            }

            try
            {
                var agentId = new AgentId(input.AgentId);
                var agent = input.Project.GetAgent(agentId);
                if (agent != null)
                {
                    output.Success = true;
                    output.Agents = new List<Nexo.Core.Domain.Entities.Agent> { agent };
                    output.Message = $"Agent {input.AgentId} found";
                }
                else
                {
                    output.Success = false;
                    output.Message = $"Agent {input.AgentId} not found";
                }
            }
            catch (Exception ex)
            {
                output.ErrorMessage = ex.Message;
            }

            return output;
        }

        private ManageProjectAgentsOutput HandleListAgents(ManageProjectAgentsInput input, ManageProjectAgentsOutput output)
        {
            try
            {
                output.Success = true;
                output.Agents = input.Project.Agents.ToList();
                output.Message = $"Found {output.Agents.Count} agents in project";
            }
            catch (Exception ex)
            {
                output.ErrorMessage = ex.Message;
            }

            return output;
        }
    }

}
