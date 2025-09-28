using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Agents
{
    /// <summary>
    /// Example demonstrating AI agents as first-class citizens in the composable command system
    /// </summary>
    public class AgentFirstClassExample
    {
        private readonly AgentComposableOrchestrator _orchestrator;
        private readonly AgentComposableCommandFactory _factory;
        private readonly IAgentOrchestrator _agentOrchestrator;
        
        public AgentFirstClassExample(
            AgentComposableOrchestrator? orchestrator = null,
            AgentComposableCommandFactory? factory = null,
            IAgentOrchestrator? agentOrchestrator = null)
        {
            _orchestrator = orchestrator ?? new AgentComposableOrchestrator();
            _factory = factory ?? new AgentComposableCommandFactory();
            _agentOrchestrator = agentOrchestrator ?? new AgentOrchestrator();
        }
        
        /// <summary>
        /// Demonstrates AI agents as first-class citizens in the composable system
        /// </summary>
        public async Task<ICommandResult> DemonstrateAgentFirstClassAsync()
        {
            try
            {
                // Create AI agents
                var codeAgent = new CodeGenerationAgent(
                    new AgentId(Guid.NewGuid().ToString()),
                    new AgentName("CodeGen Agent"),
                    PlatformType.CrossPlatform.ToString());
                
                var securityAgent = new SecurityAnalysisAgent(
                    new AgentId(Guid.NewGuid().ToString()),
                    new AgentName("Security Agent"),
                    PlatformType.CrossPlatform.ToString());
                
                var decompileAgent = new DecompilationAgent(
                    new AgentId(Guid.NewGuid().ToString()),
                    new AgentName("Decompile Agent"),
                    PlatformType.CrossPlatform.ToString());
                
                // Register agents as first-class citizens
                await _agentOrchestrator.RegisterAgentAsync(codeAgent);
                await _agentOrchestrator.RegisterAgentAsync(securityAgent);
                await _agentOrchestrator.RegisterAgentAsync(decompileAgent);
                
                // Register agents as composable commands
                var codeResult = await _orchestrator.RegisterAgentAsCommandAsync(codeAgent);
                var securityResult = await _orchestrator.RegisterAgentAsCommandAsync(securityAgent);
                var decompileResult = await _orchestrator.RegisterAgentAsCommandAsync(decompileAgent);
                
                if (!codeResult.IsSuccess || !securityResult.IsSuccess || !decompileResult.IsSuccess)
                {
                    return CommandResult.Failure(
                        new CommandIdentifier("AGENT_REGISTRATION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        "Failed to register agents as first-class citizens");
                }
                
                return CommandResult.Success(
                    new CommandIdentifier("AGENT_FIRST_CLASS", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    "AI agents are now first-class citizens in the composable command system",
                    new
                    {
                        RegisteredAgents = 3,
                        CodeAgent = codeAgent.Name.ToString(),
                        SecurityAgent = securityAgent.Name.ToString(),
                        DecompileAgent = decompileAgent.Name.ToString()
                    });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("AGENT_FIRST_CLASS_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to demonstrate agent first-class citizenship: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Demonstrates executing tasks with AI agents as composable commands
        /// </summary>
        public async Task<IReadOnlyList<ICommandResult>> DemonstrateAgentTaskExecutionAsync()
        {
            var results = new List<ICommandResult>();
            
            try
            {
                // Create tasks for different agents
                var codeTask = new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Generate C# Class"),
                    new TaskDescription("Generate a C# class for user management"),
                    TaskType.CodeGeneration,
                    TaskPriority.High,
                    "Code Generation");
                
                // Set task parameters
                codeTask.SetParameter("prompt", "Create a C# class for user management with properties and methods");
                codeTask.SetParameter("language", "C#");
                codeTask.SetParameter("framework", ".NET 8");
                
                var securityTask = new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Security Analysis"),
                    new TaskDescription("Analyze security vulnerabilities in the codebase"),
                    TaskType.SecurityAnalysis,
                    TaskPriority.High,
                    "Security Analysis");
                
                // Set task parameters
                securityTask.SetParameter("targetPath", "/path/to/codebase");
                securityTask.SetParameter("analysisType", "full");
                
                var decompileTask = new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Decompile Assembly"),
                    new TaskDescription("Decompile a .NET assembly to source code"),
                    TaskType.AssemblyAnalysis,
                    TaskPriority.Medium,
                    "Assembly Analysis");
                
                // Set task parameters
                decompileTask.SetParameter("assemblyPath", "/path/to/assembly.dll");
                decompileTask.SetParameter("outputPath", "/path/to/output");
                
                // Execute tasks with agents
                var codeResult = await _orchestrator.ExecuteTaskWithBestAgentAsync(codeTask);
                var securityResult = await _orchestrator.ExecuteTaskWithBestAgentAsync(securityTask);
                var decompileResult = await _orchestrator.ExecuteTaskWithBestAgentAsync(decompileTask);
                
                results.Add(codeResult);
                results.Add(securityResult);
                results.Add(decompileResult);
                
                return results;
            }
            catch (Exception ex)
            {
                results.Add(CommandResult.Failure(
                    new CommandIdentifier("TASK_EXECUTION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to execute agent tasks: {ex.Message}"));
                return results;
            }
        }
        
        /// <summary>
        /// Demonstrates AI agent workflow execution
        /// </summary>
        public async Task<IWorkflowResult> DemonstrateAgentWorkflowAsync()
        {
            try
            {
                // Create a workflow with multiple agent tasks
                var tasks = new List<AgentTask>
                {
                    new AgentTask(
                        new TaskId(Guid.NewGuid().ToString()),
                        new TaskName("Code Generation"),
                        new TaskDescription("Generate initial code"),
                        TaskType.CodeGeneration,
                        TaskPriority.High,
                        "Code Generation"),
                    new AgentTask(
                        new TaskId(Guid.NewGuid().ToString()),
                        new TaskName("Security Analysis"),
                        new TaskDescription("Analyze generated code for security issues"),
                        TaskType.SecurityAnalysis,
                        TaskPriority.High,
                        "Security Analysis"),
                    new AgentTask(
                        new TaskId(Guid.NewGuid().ToString()),
                        new TaskName("Code Review"),
                        new TaskDescription("Review and improve the code"),
                        TaskType.CodeAnalysis,
                        TaskPriority.Medium,
                        "Code Analysis")
                };
                
                // Execute workflow
                var workflowResult = await _orchestrator.CreateAgentWorkflowAsync(
                    "AI Development Workflow",
                    tasks);
                
                return workflowResult;
            }
            catch (Exception ex)
            {
                return new WorkflowResult(
                    false,
                    $"Agent workflow failed: {ex.Message}",
                    new List<ICommandResult>(),
                    new Dictionary<string, object>(),
                    DateTime.UtcNow,
                    TimeSpan.Zero);
            }
        }
        
        /// <summary>
        /// Demonstrates agent capabilities and discovery
        /// </summary>
        public async Task<ICommandResult> DemonstrateAgentCapabilitiesAsync()
        {
            try
            {
                // Get all registered agents
                var agents = await _agentOrchestrator.GetRegisteredAgentsAsync();
                
                // Get all agent commands
                var agentCommands = await _orchestrator.GetAgentCommandsAsync();
                
                // Analyze capabilities
                var capabilities = new Dictionary<string, int>();
                var platforms = new Dictionary<string, int>();
                
                foreach (var agent in agents)
                {
                    // Count capabilities
                    foreach (var capability in agent.Capabilities)
                    {
                        if (capabilities.ContainsKey(capability.Category))
                        {
                            capabilities[capability.Category]++;
                        }
                        else
                        {
                            capabilities[capability.Category] = 1;
                        }
                    }
                    
                    // Count platforms
                    var platformName = agent.Platform.ToString();
                    if (platforms.ContainsKey(platformName))
                    {
                        platforms[platformName]++;
                    }
                    else
                    {
                        platforms[platformName] = 1;
                    }
                }
                
                return CommandResult.Success(
                    new CommandIdentifier("AGENT_CAPABILITIES", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    "Agent capabilities analysis completed",
                    new
                    {
                        TotalAgents = agents.Count,
                        TotalCommands = agentCommands.Count,
                        Capabilities = capabilities,
                        Platforms = platforms,
                        AgentDetails = agents.Select(a => new
                        {
                            Id = a.Id.ToString(),
                            Name = a.Name.ToString(),
                            Status = a.Status.ToString(),
                            Platform = a.Platform.ToString(),
                            CapabilityCount = a.Capabilities.Count,
                            FocusAreas = a.FocusAreas.Count
                        }).ToList()
                    });
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("CAPABILITIES_ANALYSIS_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to analyze agent capabilities: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Demonstrates agent communication and coordination
        /// </summary>
        public async Task<ICommandResult> DemonstrateAgentCommunicationAsync()
        {
            try
            {
                // Get agents
                var agents = await _agentOrchestrator.GetRegisteredAgentsAsync();
                if (agents.Count < 2)
                {
                    return CommandResult.Failure(
                        new CommandIdentifier("INSUFFICIENT_AGENTS", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        "At least 2 agents required for communication demonstration");
                }
                
                var agent1 = agents[0];
                var agent2 = agents[1];
                
                // Create communication message
                var message = AgentMessage.CreateCapabilityRequest(
                    agent1.Id,
                    agent2.Id,
                    "Code Generation");
                
                // Send message
                var communicationResult = await agent1.CommunicateAsync(message);
                
                if (communicationResult.IsSuccess)
                {
                    return CommandResult.Success(
                        new CommandIdentifier("AGENT_COMMUNICATION", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        "Agent communication successful",
                        new
                        {
                            Sender = agent1.Name.ToString(),
                            Receiver = agent2.Name.ToString(),
                            MessageType = message.Type.ToString(),
                            CommunicationResult = communicationResult
                        });
                }
                else
                {
                    return CommandResult.Failure(
                        new CommandIdentifier("COMMUNICATION_FAILED", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                        $"Agent communication failed: {communicationResult.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                return CommandResult.Failure(
                    new CommandIdentifier("COMMUNICATION_ERROR", "Nexo.Shared.Composable.Agents", "1.0.0"), 
                    $"Failed to demonstrate agent communication: {ex.Message}");
            }
        }
    }
}
