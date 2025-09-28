using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Nexo.Core.Domain.Agents;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Core.Domain.Values;
using Nexo.Shared.Values;
using Nexo.Shared.Composable.Agents;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Tests.Composable
{
    /// <summary>
    /// Tests for AI agent integration with composable commands
    /// </summary>
    public class AgentIntegrationTests
    {
        private readonly AgentComposableOrchestrator _orchestrator;
        private readonly AgentComposableCommandFactory _factory;
        private readonly ICollectionOperations _collectionOps;
        private readonly ILoopOperations _loopOps;
        private readonly IStringOperations _stringOps;
        
        public AgentIntegrationTests()
        {
            _collectionOps = new CollectionOperations();
            _loopOps = new LoopOperations();
            _stringOps = new StringOperations();
            _orchestrator = new AgentComposableOrchestrator(
                collectionOps: _collectionOps,
                loopOps: _loopOps);
            _factory = new AgentComposableCommandFactory();
        }
        
        [Fact]
        public async Task RegisterAgentAsCommandAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            // Act
            var result = await _orchestrator.RegisterAgentAsCommandAsync(agent);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Agent registered as composable command successfully", result.Message);
        }
        
        [Fact]
        public async Task RegisterAgentAsCommandAsync_WithNullAgent_ShouldFail()
        {
            // Act
            var result = await _orchestrator.RegisterAgentAsCommandAsync(null);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Agent cannot be null", result.Message);
        }
        
        [Fact]
        public async Task ExecuteTaskWithBestAgentAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            await _orchestrator.RegisterAgentAsCommandAsync(agent);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task description"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object> { ["testParam"] = "testValue" });
            
            // Act
            var result = await _orchestrator.ExecuteTaskWithBestAgentAsync(task);
            
            // Assert
            Assert.True(result.IsSuccess);
        }
        
        [Fact]
        public async Task ExecuteTaskWithBestAgentAsync_NoSuitableAgent_ShouldFail()
        {
            // Arrange
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task description"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            // Act
            var result = await _orchestrator.ExecuteTaskWithBestAgentAsync(task);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("No suitable agent found", result.Message);
        }
        
        [Fact]
        public async Task ExecuteTaskWithAgentAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            await _orchestrator.RegisterAgentAsCommandAsync(agent);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task description"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            // Act
            var result = await _orchestrator.ExecuteTaskWithAgentAsync(task, agent.Id);
            
            // Assert
            Assert.True(result.IsSuccess);
        }
        
        [Fact]
        public async Task ExecuteTaskWithAgentAsync_NonExistentAgent_ShouldFail()
        {
            // Arrange
            var agentId = new AgentId(Guid.NewGuid().ToString());
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task description"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            // Act
            var result = await _orchestrator.ExecuteTaskWithAgentAsync(task, agentId);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Agent", result.Message);
            Assert.Contains("not found", result.Message);
        }
        
        [Fact]
        public async Task ExecuteTasksWithAgentsAsync_ShouldExecuteAllTasks()
        {
            // Arrange
            var agent = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            await _orchestrator.RegisterAgentAsCommandAsync(agent);
            
            var tasks = new List<AgentTask>
            {
                new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Task 1"),
                    new TaskDescription("First task"),
                    TaskType.General,
                    TaskPriority.Medium,
                    new Dictionary<string, object>()),
                new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Task 2"),
                    new TaskDescription("Second task"),
                    TaskType.General,
                    TaskPriority.Medium,
                    new Dictionary<string, object>())
            };
            
            // Act
            var results = await _orchestrator.ExecuteTasksWithAgentsAsync(tasks);
            
            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
        
        [Fact]
        public async Task CreateAgentWorkflowAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            await _orchestrator.RegisterAgentAsCommandAsync(agent);
            
            var tasks = new List<AgentTask>
            {
                new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Workflow Task 1"),
                    new TaskDescription("First workflow task"),
                    TaskType.General,
                    TaskPriority.Medium,
                    new Dictionary<string, object>()),
                new AgentTask(
                    new TaskId(Guid.NewGuid().ToString()),
                    new TaskName("Workflow Task 2"),
                    new TaskDescription("Second workflow task"),
                    TaskType.General,
                    TaskPriority.Medium,
                    new Dictionary<string, object>())
            };
            
            // Act
            var workflowResult = await _orchestrator.CreateAgentWorkflowAsync(
                "Test Workflow",
                tasks);
            
            // Assert
            Assert.True(workflowResult.IsSuccess);
            Assert.Contains("Test Workflow", workflowResult.Message);
        }
        
        [Fact]
        public async Task GetAgentCommandsAsync_ShouldReturnAgentCommands()
        {
            // Arrange
            var agent1 = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Agent 1"),
                PlatformType.CrossPlatform);
            
            var agent2 = new TestAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Agent 2"),
                PlatformType.CrossPlatform);
            
            await _orchestrator.RegisterAgentAsCommandAsync(agent1);
            await _orchestrator.RegisterAgentAsCommandAsync(agent2);
            
            // Act
            var agentCommands = await _orchestrator.GetAgentCommandsAsync();
            
            // Assert
            Assert.Equal(2, agentCommands.Count);
            Assert.All(agentCommands, command => Assert.IsAssignableFrom<IAgentComposableCommand>(command));
        }
        
        [Fact]
        public async Task CreateCodeGenerationCommandAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Code Gen Agent"),
                PlatformType.CrossPlatform);
            
            // Act
            var command = await _factory.CreateCodeGenerationCommandAsync(
                agent,
                "Create a user class",
                "C#",
                ".NET 8");
            
            // Assert
            Assert.NotNull(command);
            Assert.Equal(agent, command.Agent);
            Assert.NotNull(command.AgentTask);
            Assert.NotNull(command.AgentContext);
        }
        
        [Fact]
        public async Task CreateSecurityAnalysisCommandAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestSecurityAnalysisAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Security Agent"),
                PlatformType.CrossPlatform);
            
            // Act
            var command = await _factory.CreateSecurityAnalysisCommandAsync(
                agent,
                "/path/to/codebase",
                "full");
            
            // Assert
            Assert.NotNull(command);
            Assert.Equal(agent, command.Agent);
            Assert.NotNull(command.AgentTask);
            Assert.NotNull(command.AgentContext);
        }
        
        [Fact]
        public async Task CreateDecompilationCommandAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestDecompilationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Decompile Agent"),
                PlatformType.CrossPlatform);
            
            // Act
            var command = await _factory.CreateDecompilationCommandAsync(
                agent,
                "/path/to/assembly.dll",
                "/path/to/output");
            
            // Assert
            Assert.NotNull(command);
            Assert.Equal(agent, command.Agent);
            Assert.NotNull(command.AgentTask);
            Assert.NotNull(command.AgentContext);
        }
    }
    
    /// <summary>
    /// Test implementation of AI agent
    /// </summary>
    public class TestAgent : IAgent
    {
        public AgentId Id { get; }
        public AgentName Name { get; }
        public AgentStatus Status { get; private set; }
        public PlatformType Platform { get; }
        public IReadOnlyList<AgentCapability> Capabilities { get; }
        public IReadOnlyList<string> FocusAreas { get; }
        public IReadOnlyDictionary<string, object> Configuration { get; }
        
        public TestAgent(AgentId id, AgentName name, PlatformType platform)
        {
            Id = id;
            Name = name;
            Platform = platform;
            Status = AgentStatus.Inactive;
            Capabilities = new List<AgentCapability>
            {
                AgentCapability.CodeGeneration,
                AgentCapability.CodeAnalysis
            };
            FocusAreas = new List<string> { "Testing", "Development" };
            Configuration = new Dictionary<string, object>();
        }
        
        public async Task<AgentResult> InitializeAsync(AgentContext context)
        {
            Status = AgentStatus.Active;
            return AgentResult.Success("Agent initialized", agentId: Id, operationType: "Initialize");
        }
        
        public async Task<AgentResult> ExecuteAsync(AgentTask task)
        {
            return AgentResult.Success("Task executed successfully", agentId: Id, operationType: "Execute");
        }
        
        public async Task<AgentResult> CommunicateAsync(IAgent targetAgent, AgentMessage message)
        {
            return AgentResult.Success("Communication successful", agentId: Id, operationType: "Communicate");
        }
        
        public async Task<AgentResult> LearnAsync(AgentExperience experience)
        {
            return AgentResult.Success("Learning completed", agentId: Id, operationType: "Learn");
        }
        
        public async Task<AgentResult> ShutdownAsync()
        {
            Status = AgentStatus.Inactive;
            return AgentResult.Success("Agent shutdown", agentId: Id, operationType: "Shutdown");
        }
        
        public AgentState GetState()
        {
            return new AgentState(Id, Status, new Dictionary<string, object>());
        }
        
        public async Task<AgentResult> RestoreAsync(AgentState state)
        {
            Status = state.Status;
            return AgentResult.Success("Agent restored", agentId: Id, operationType: "Restore");
        }
    }
    
    /// <summary>
    /// Test implementation of code generation agent
    /// </summary>
    public class TestCodeGenerationAgent : TestAgent
    {
        public TestCodeGenerationAgent(AgentId id, AgentName name, PlatformType platform) 
            : base(id, name, platform)
        {
        }
    }
    
    /// <summary>
    /// Test implementation of security analysis agent
    /// </summary>
    public class TestSecurityAnalysisAgent : TestAgent
    {
        public TestSecurityAnalysisAgent(AgentId id, AgentName name, PlatformType platform) 
            : base(id, name, platform)
        {
        }
    }
    
    /// <summary>
    /// Test implementation of decompilation agent
    /// </summary>
    public class TestDecompilationAgent : TestAgent
    {
        public TestDecompilationAgent(AgentId id, AgentName name, PlatformType platform) 
            : base(id, name, platform)
        {
        }
    }
}
