using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Orchestrator that bridges AI agents with the composable command system
    /// </summary>
    public class AgentComposableOrchestrator
    {
        private readonly IComposableOrchestrator _commandOrchestrator;
        private readonly IAgentOrchestrator _agentOrchestrator;
        private readonly AgentComposableCommandFactory _agentCommandFactory;
        private readonly ICollectionOperations _collectionOps;
        private readonly ILoopOperations _loopOps;
        
        public AgentComposableOrchestrator(
            IComposableOrchestrator? commandOrchestrator = null,
            IAgentOrchestrator? agentOrchestrator = null,
            AgentComposableCommandFactory? agentCommandFactory = null,
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null)
        {
            _commandOrchestrator = commandOrchestrator ?? new ComposableOrchestrator();
            _agentOrchestrator = agentOrchestrator ?? new AgentOrchestrator();
            _agentCommandFactory = agentCommandFactory ?? new AgentComposableCommandFactory();
            _collectionOps = collectionOps ?? new CollectionOperations();
            _loopOps = loopOps ?? new LoopOperations();
        }
        
        /// <summary>
        /// Registers an AI agent as a composable command
        /// </summary>
        public async Task<ICommandResult> RegisterAgentAsCommandAsync(IAgent agent)
        {
            try
            {
                // Register the agent with the agent orchestrator
                await _agentOrchestrator.RegisterAgentAsync(agent);
                
                // Create a generic agent command
                var agentTask = new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Generic Agent Task"),
                    new TaskDescription("Generic task for agent execution"),
                    TaskType.General,
                    TaskPriority.Medium,
                    "General");
                
                var agentContext = new AgentContext(agent.Platform);
                var agentCommand = await _agentCommandFactory.CreateAgentCommandAsync(agent, agentTask, agentContext);
                
                // Register the agent command with the command orchestrator
                var commandResult = await _commandOrchestrator.RegisterCommandAsync(agentCommand);
                if (!commandResult.IsSuccess)
                {
                    // Rollback agent registration
                    await _agentOrchestrator.UnregisterAgentAsync(agent.Id);
                    return commandResult;
                }
                
                return CommandResult.Success(
                    new CommandIdentifier($"Agent_{agent.Id}", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    "Agent registered as composable command successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("AGENT_REGISTRATION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to register agent as command: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Executes a task using the best available agent
        /// </summary>
        public async Task<ICommandResult> ExecuteTaskWithBestAgentAsync(AgentTask task)
        {
            try
            {
                // Find the best agent for the task
                var bestAgent = await _agentOrchestrator.FindBestAgentForTaskAsync(task);
                if (bestAgent == null)
                {
                    return CommandResult.Failure(
                        new CommandIdentifier("NO_AGENT_FOUND", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        "No suitable agent found for the task");
                }
                
                // Create agent command
                var agentContext = new AgentContext(bestAgent.Platform);
                var agentCommand = await _agentCommandFactory.CreateAgentCommandAsync(bestAgent, task, agentContext);
                
                // Register the command temporarily
                await _commandOrchestrator.RegisterCommandAsync(agentCommand);
                
                // Execute the command
                var context = CommandContext.Create(agentCommand.Id, ("task", task));
                var result = await _commandOrchestrator.ExecuteCommandAsync(agentCommand.Id, context);
                
                // Unregister the command
                await _commandOrchestrator.UnregisterCommandAsync(agentCommand.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("TASK_EXECUTION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to execute task with agent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Executes a task using a specific agent
        /// </summary>
        public async Task<ICommandResult> ExecuteTaskWithAgentAsync(AgentTask task, AgentId agentId)
        {
            try
            {
                // Get the specific agent
                var agent = await _agentOrchestrator.GetAgentAsync(agentId);
                if (agent == null)
                {
                    return CommandResult.Failure(
                        new CommandIdentifier("AGENT_NOT_FOUND", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        $"Agent {agentId} not found");
                }
                
                // Create agent command
                var agentContext = new AgentContext(agent.Platform);
                var agentCommand = await _agentCommandFactory.CreateAgentCommandAsync(agent, task, agentContext);
                
                // Register the command temporarily
                await _commandOrchestrator.RegisterCommandAsync(agentCommand);
                
                // Execute the command
                var context = CommandContext.Create(agentCommand.Id, ("task", task));
                var result = await _commandOrchestrator.ExecuteCommandAsync(agentCommand.Id, context);
                
                // Unregister the command
                await _commandOrchestrator.UnregisterCommandAsync(agentCommand.Id);
                
                return result;
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("TASK_EXECUTION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to execute task with specific agent: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Executes multiple tasks using multiple agents in parallel
        /// </summary>
        public async Task<IReadOnlyList<ICommandResult>> ExecuteTasksWithAgentsAsync(IReadOnlyList<AgentTask> tasks)
        {
            var results = new List<ICommandResult>();
            
            await _loopOps.ForEachAsync(tasks, async (task, index) =>
            {
                var result = await ExecuteTaskWithBestAgentAsync(task);
                results.Add(result);
            });
            
            return results;
        }
        
        /// <summary>
        /// Creates a workflow that uses AI agents
        /// </summary>
        public async Task<IWorkflowResult> CreateAgentWorkflowAsync(
            string workflowName,
            IReadOnlyList<AgentTask> tasks,
            IReadOnlyList<AgentId>? agentIds = null)
        {
            try
            {
                var workflowSteps = new List<IWorkflowStep>();
                var commandIds = new List<ICommandIdentifier>();
                
                // Create commands for each task
                await _loopOps.ForEachAsync(tasks, async (task, index) =>
                {
                    IAgent agent;
                    
                    if (agentIds != null && index < agentIds.Count)
                    {
                        // Use specific agent
                        agent = await _agentOrchestrator.GetAgentAsync(agentIds[index]);
                        if (agent == null)
                        {
                            throw new InvalidOperationException($"Agent {agentIds[index]} not found");
                        }
                    }
                    else
                    {
                        // Find best agent for task
                        agent = await _agentOrchestrator.FindBestAgentForTaskAsync(task);
                        if (agent == null)
                        {
                            throw new InvalidOperationException($"No suitable agent found for task {task.Name}");
                        }
                    }
                    
                    // Create agent command
                    var agentContext = new AgentContext(agent.Platform);
                    var agentCommand = await _agentCommandFactory.CreateAgentCommandAsync(agent, task, agentContext);
                    
                    // Register command
                    await _commandOrchestrator.RegisterCommandAsync(agentCommand);
                    commandIds.Add(agentCommand.Id);
                    
                    // Create workflow step
                    var step = new WorkflowStep(
                        $"Step_{index + 1}",
                        $"Execute {task.Name}",
                        $"Execute task {task.Name} using agent {agent.Name}",
                        "AgentTask",
                        "Pending",
                        index + 1);
                    
                    workflowSteps.Add(step);
                });
                
                // Create workflow definition
                var workflow = new WorkflowDefinition(
                    workflowName,
                    $"AI agent workflow with {tasks.Count} tasks",
                    $"AI agent workflow with {tasks.Count} tasks",
                    workflowSteps);
                
                // Execute workflow
                var context = CommandContext.Create(commandIds.First(), ("workflow", workflowName));
                var result = await _commandOrchestrator.ExecuteWorkflowAsync(workflow, context);
                
                // Clean up commands
                await _loopOps.ForEachAsync(commandIds, async (commandId, index) =>
                {
                    await _commandOrchestrator.UnregisterCommandAsync(commandId);
                });
                
                return result;
            }
            catch (Exception ex)
            {
                return new WorkflowResult(
                    false,
                    $"Agent workflow execution failed: {ex.Message}",
                    new List<ICommandResult>(),
                    new Dictionary<string, object>(),
                    DateTime.UtcNow,
                    TimeSpan.Zero);
            }
        }
        
        /// <summary>
        /// Gets all registered agents as composable commands
        /// </summary>
        public async Task<IReadOnlyList<IComposableCommand>> GetAgentCommandsAsync()
        {
            var commands = await _commandOrchestrator.GetRegisteredCommandsAsync();
            var agentCommands = new List<IComposableCommand>();
            
            await _loopOps.ForEachAsync(commands, async (command, index) =>
            {
                if (command is IAgentComposableCommand agentCommand)
                {
                    agentCommands.Add(agentCommand);
                }
            });
            
            return agentCommands;
        }
    }
}
