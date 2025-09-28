using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Domain.Entities;
using Nexo.Core.Domain.Values;

namespace Nexo.Core.Application.Commands.Project
{
    /// <summary>
    /// Orchestrator that composes project-related commands into workflows
    /// This replaces the large Project class by composing small, focused commands
    /// </summary>
    public class ProjectOrchestrator
    {
        private readonly ICommandOrchestrator<CreateProjectInput, CreateProjectOutput> _createOrchestrator;
        private readonly ICommandOrchestrator<ManageProjectAgentsInput, ManageProjectAgentsOutput> _agentOrchestrator;
        private readonly ICommandOrchestrator<CalculateProjectMetricsInput, CalculateProjectMetricsOutput> _metricsOrchestrator;

        public ProjectOrchestrator(
            ICommandOrchestrator<CreateProjectInput, CreateProjectOutput> createOrchestrator,
            ICommandOrchestrator<ManageProjectAgentsInput, ManageProjectAgentsOutput> agentOrchestrator,
            ICommandOrchestrator<CalculateProjectMetricsInput, CalculateProjectMetricsOutput> metricsOrchestrator)
        {
            _createOrchestrator = createOrchestrator;
            _agentOrchestrator = agentOrchestrator;
            _metricsOrchestrator = metricsOrchestrator;
        }

        /// <summary>
        /// Creates a new project with initial setup
        /// </summary>
        public async Task<ProjectCreationResult> CreateProjectAsync(CreateProjectInput input, CancellationToken cancellationToken = default)
        {
            var commands = new List<ICommand<CreateProjectInput, CreateProjectOutput>>
            {
                new CreateProjectCommand()
            };

            var result = await _createOrchestrator.ExecuteAsync(commands, input, cancellationToken: cancellationToken);

            return new ProjectCreationResult
            {
                IsSuccess = result.IsSuccess,
                Project = result.FinalResult?.Project,
                ProjectId = result.FinalResult?.ProjectId,
                Errors = result.Errors
            };
        }

        /// <summary>
        /// Manages project agents (add, remove, get, list)
        /// </summary>
        public async Task<ProjectAgentManagementResult> ManageProjectAgentsAsync(ManageProjectAgentsInput input, CancellationToken cancellationToken = default)
        {
            var commands = new List<ICommand<ManageProjectAgentsInput, ManageProjectAgentsOutput>>
            {
                new ManageProjectAgentsCommand()
            };

            var result = await _agentOrchestrator.ExecuteAsync(commands, input, cancellationToken: cancellationToken);

            return new ProjectAgentManagementResult
            {
                IsSuccess = result.IsSuccess,
                Agents = result.FinalResult?.Agents ?? new List<Nexo.Core.Domain.Entities.Agent>(),
                Message = result.FinalResult?.Message,
                Errors = result.Errors
            };
        }

        /// <summary>
        /// Calculates project metrics
        /// </summary>
        public async Task<CalculateProjectMetricsOutput> CalculateProjectMetricsAsync(CalculateProjectMetricsInput input, CancellationToken cancellationToken = default)
        {
            var commands = new List<ICommand<CalculateProjectMetricsInput, CalculateProjectMetricsOutput>>
            {
                new CalculateProjectMetricsCommand()
            };

            var result = await _metricsOrchestrator.ExecuteAsync(commands, input, cancellationToken: cancellationToken);
            return result.FinalResult ?? new CalculateProjectMetricsOutput();
        }

        /// <summary>
        /// Executes a complete project workflow
        /// </summary>
        public async Task<ProjectWorkflowResult> ExecuteProjectWorkflowAsync(ProjectWorkflowInput input, CancellationToken cancellationToken = default)
        {
            var results = new List<object>();
            var errors = new List<string>();
            var warnings = new List<string>();

            try
            {
                // Step 1: Create project if needed
                if (input.CreateProject)
                {
                    var createResult = await CreateProjectAsync(input.CreateInput, cancellationToken);
                    if (createResult.IsSuccess)
                    {
                        results.Add(createResult);
                        input.Project = createResult.Project;
                    }
                    else
                    {
                        errors.AddRange(createResult.Errors);
                        return new ProjectWorkflowResult
                        {
                            IsSuccess = false,
                            Results = results,
                            Errors = errors,
                            Warnings = warnings
                        };
                    }
                }

                // Step 2: Add agents if specified
                if (input.AgentsToAdd?.Any() == true && input.Project != null)
                {
                    foreach (var agent in input.AgentsToAdd)
                    {
                        var agentInput = new ManageProjectAgentsInput
                        {
                            ProjectId = input.Project.Id.ToString(),
                            Project = input.Project,
                            Action = AgentAction.Add,
                            Agent = agent
                        };

                        var agentResult = await ManageProjectAgentsAsync(agentInput, cancellationToken);
                        if (agentResult.IsSuccess)
                        {
                            results.Add(agentResult);
                        }
                        else
                        {
                            warnings.Add($"Failed to add agent {agent.Id}: {string.Join(", ", agentResult.Errors)}");
                        }
                    }
                }

                // Step 3: Calculate metrics if requested
                if (input.CalculateMetrics && input.Project != null)
                {
                    var metricsInput = new CalculateProjectMetricsInput
                    {
                        ProjectId = input.Project.Id.ToString(),
                        Project = input.Project,
                        IncludeDetailedMetrics = true
                    };

                    var metricsResult = await CalculateProjectMetricsAsync(metricsInput, cancellationToken);
                    results.Add(metricsResult);
                }

                return new ProjectWorkflowResult
                {
                    IsSuccess = errors.Count == 0,
                    Results = results,
                    Errors = errors,
                    Warnings = warnings
                };
            }
            catch (Exception ex)
            {
                errors.Add($"Project workflow failed: {ex.Message}");
                return new ProjectWorkflowResult
                {
                    IsSuccess = false,
                    Results = results,
                    Errors = errors,
                    Warnings = warnings
                };
            }
        }
    }

}
