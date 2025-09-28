using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Commands;
using Nexo.Core.Application.Commands.Assembly;
using Nexo.Core.Application.Commands.Project;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Shared.Values;

namespace Nexo.Core.Application.Services
{
    /// <summary>
    /// Main application service that orchestrates commands and agents
    /// </summary>
    public class ApplicationService
    {
        private readonly IAgentService _agentService;
        private readonly ValidationService _validationService;
        private readonly ICommandOrchestrator<object, object> _commandOrchestrator;

        public ApplicationService(
            IAgentService agentService,
            ValidationService validationService,
            ICommandOrchestrator<object, object> commandOrchestrator)
        {
            _agentService = agentService;
            _validationService = validationService;
            _commandOrchestrator = commandOrchestrator;
        }

        /// <summary>
        /// Creates a new project with agents
        /// </summary>
        public async Task<ApplicationResult> CreateProjectWithAgentsAsync(
            string projectName,
            string projectPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Step 1: Create the project
                var createProjectCommand = new CreateProjectCommand();
                var projectInput = new CreateProjectInput
                {
                    Name = projectName,
                    Path = projectPath,
                    Metadata = new Dictionary<string, object>
                    {
                        ["CreatedBy"] = "ApplicationService",
                        ["CreatedAt"] = DateTime.UtcNow
                    }
                };

                var projectResult = await createProjectCommand.ExecuteAsync(projectInput, cancellationToken);
                if (!projectResult.IsSuccess)
                {
                    return ApplicationResult.Failure($"Failed to create project: {projectResult.ErrorMessage}");
                }

                // Step 2: Create agents for the project
                var agents = await CreateProjectAgentsAsync(cancellationToken);
                var agentResults = new List<AgentResult>();

                foreach (var agent in agents)
                {
                    var agentResult = await _agentService.CreateAgentAsync(agent, cancellationToken);
                    agentResults.Add(agentResult);
                }

                var failedAgents = agentResults.Where(r => !r.IsSuccess).ToList();
                if (failedAgents.Any())
                {
                    return ApplicationResult.Failure($"Failed to create some agents: {string.Join(", ", failedAgents.Select(r => r.ErrorMessage))}");
                }

                return ApplicationResult.Success($"Project '{projectName}' created successfully with {agents.Count} agents");
            }
            catch (Exception ex)
            {
                return ApplicationResult.Failure($"Failed to create project: {ex.Message}");
            }
        }

        /// <summary>
        /// Analyzes an assembly using agents
        /// </summary>
        public async Task<ApplicationResult> AnalyzeAssemblyAsync(
            string assemblyPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Get available agents
                var agents = await _agentService.GetAllAgentsAsync(cancellationToken);
                var analysisAgents = agents.Where(a => a.Capabilities.Any(c => c.Name == "AssemblyAnalysis")).ToList();

                if (!analysisAgents.Any())
                {
                    return ApplicationResult.Failure("No analysis agents available");
                }

                // Create analysis command
                var analyzeCommand = new AnalyzeAssemblyCommand();
                var analysisInput = new AnalyzeAssemblyInput
                {
                    AssemblyPath = assemblyPath,
                    IncludePrivateMembers = false,
                    IncludeInternalMembers = false,
                    IncludeNestedTypes = true,
                    IncludeGenerics = true,
                    IncludeResources = true,
                    IncludeCustomAttributes = true,
                    IncludeDependencies = true
                };

                var analysisResult = await analyzeCommand.ExecuteAsync(analysisInput, cancellationToken);
                if (!analysisResult.IsSuccess)
                {
                    return ApplicationResult.Failure($"Assembly analysis failed: {analysisResult.ErrorMessage}");
                }

                return ApplicationResult.Success($"Assembly '{assemblyPath}' analyzed successfully");
            }
            catch (Exception ex)
            {
                return ApplicationResult.Failure($"Failed to analyze assembly: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a workflow of commands
        /// </summary>
        public async Task<ApplicationResult> ExecuteWorkflowAsync(
            IEnumerable<ICommand<object, object>> commands,
            object input,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _commandOrchestrator.ExecuteAsync(commands, input, cancellationToken: cancellationToken);
                
                if (result.IsSuccess)
                {
                    return ApplicationResult.Success($"Workflow executed successfully in {result.TotalExecutionTime.TotalMilliseconds}ms");
                }
                else
                {
                    return ApplicationResult.Failure($"Workflow failed: {string.Join(", ", result.Errors)}");
                }
            }
            catch (Exception ex)
            {
                return ApplicationResult.Failure($"Workflow execution failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates agents for a project
        /// </summary>
        private Task<List<CreateAgentRequest>> CreateProjectAgentsAsync(CancellationToken cancellationToken)
        {
            var agents = new List<CreateAgentRequest>();

            // Create a code generation agent
            agents.Add(new CreateAgentRequest
            {
                Id = new AgentId(Guid.NewGuid().ToString()),
                Name = new AgentName("CodeGenerator"),
                Type = AgentType.CodeGeneration,
                Platform = PlatformType.CrossPlatform,
                Capabilities = new List<AgentCapability> { new AgentCapability("CodeGeneration", "Code generation capability", "Development") },
                FocusAreas = new List<string> { "C#", "TypeScript", "Python" },
                Configuration = new Dictionary<string, object>
                {
                    ["Language"] = "C#",
                    ["Framework"] = ".NET 8.0"
                }
            });

            // Create a security analysis agent
            agents.Add(new CreateAgentRequest
            {
                Id = new AgentId(Guid.NewGuid().ToString()),
                Name = new AgentName("SecurityAnalyzer"),
                Type = AgentType.SecurityAnalysis,
                Platform = PlatformType.CrossPlatform,
                Capabilities = new List<AgentCapability> { new AgentCapability("SecurityAnalysis", "Security analysis capability", "Security") },
                FocusAreas = new List<string> { "Vulnerability Detection", "Code Security" },
                Configuration = new Dictionary<string, object>
                {
                    ["ScanLevel"] = "Comprehensive",
                    ["ReportFormat"] = "Detailed"
                }
            });

            return Task.FromResult(agents);
        }
    }

    /// <summary>
    /// Result of an application operation
    /// </summary>
    public class ApplicationResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }

        public static ApplicationResult Success(string message, Dictionary<string, object>? metadata = null)
        {
            return new ApplicationResult
            {
                IsSuccess = true,
                Message = message,
                Metadata = metadata
            };
        }

        public static ApplicationResult Failure(string errorMessage, Dictionary<string, object>? metadata = null)
        {
            return new ApplicationResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Metadata = metadata
            };
        }
    }
}
