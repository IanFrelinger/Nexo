using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Tests for AI agents as first-class citizens
    /// </summary>
    public class AgentFirstClassTests
    {
        private readonly AgentFirstClassExample _example;
        private readonly AgentComposableOrchestrator _orchestrator;
        private readonly IAgentOrchestrator _agentOrchestrator;
        
        public AgentFirstClassTests()
        {
            _agentOrchestrator = new AgentOrchestrator();
            _orchestrator = new AgentComposableOrchestrator(agentOrchestrator: _agentOrchestrator);
            _example = new AgentFirstClassExample(_orchestrator);
        }
        
        [Fact]
        public async Task DemonstrateAgentFirstClassAsync_ShouldSucceed()
        {
            // Act
            var result = await _example.DemonstrateAgentFirstClassAsync();
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("AI agents are now first-class citizens", result.Message);
            
            // Verify agents are registered
            var agents = await _agentOrchestrator.GetRegisteredAgentsAsync();
            Assert.True(agents.Count >= 3);
        }
        
        [Fact]
        public async Task DemonstrateAgentTaskExecutionAsync_ShouldSucceed()
        {
            // Arrange - Register some agents first
            var codeAgent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Code Agent"),
                PlatformType.CrossPlatform);
            
            var securityAgent = new TestSecurityAnalysisAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Security Agent"),
                PlatformType.CrossPlatform);
            
            await _agentOrchestrator.RegisterAgentAsync(codeAgent);
            await _agentOrchestrator.RegisterAgentAsync(securityAgent);
            
            // Act
            var results = await _example.DemonstrateAgentTaskExecutionAsync();
            
            // Assert
            Assert.True(results.Count > 0);
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
        
        [Fact]
        public async Task DemonstrateAgentWorkflowAsync_ShouldSucceed()
        {
            // Arrange - Register agents first
            var codeAgent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Code Agent"),
                PlatformType.CrossPlatform);
            
            var securityAgent = new TestSecurityAnalysisAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Security Agent"),
                PlatformType.CrossPlatform);
            
            await _agentOrchestrator.RegisterAgentAsync(codeAgent);
            await _agentOrchestrator.RegisterAgentAsync(securityAgent);
            
            // Act
            var workflowResult = await _example.DemonstrateAgentWorkflowAsync();
            
            // Assert
            Assert.True(workflowResult.IsSuccess);
            Assert.Contains("AI Development Workflow", workflowResult.Message);
        }
        
        [Fact]
        public async Task DemonstrateAgentCapabilitiesAsync_ShouldSucceed()
        {
            // Arrange - Register some agents
            var codeAgent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Code Agent"),
                PlatformType.CrossPlatform);
            
            var securityAgent = new TestSecurityAnalysisAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Security Agent"),
                PlatformType.CrossPlatform);
            
            await _agentOrchestrator.RegisterAgentAsync(codeAgent);
            await _agentOrchestrator.RegisterAgentAsync(securityAgent);
            
            // Act
            var result = await _example.DemonstrateAgentCapabilitiesAsync();
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Agent capabilities analysis completed", result.Message);
            
            // Verify capabilities data
            var data = result.Data as dynamic;
            Assert.NotNull(data);
            Assert.True(data.TotalAgents > 0);
            Assert.True(data.TotalCommands > 0);
        }
        
        [Fact]
        public async Task DemonstrateAgentCommunicationAsync_ShouldSucceed()
        {
            // Arrange - Register at least 2 agents
            var agent1 = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Agent 1"),
                PlatformType.CrossPlatform);
            
            var agent2 = new TestSecurityAnalysisAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Agent 2"),
                PlatformType.CrossPlatform);
            
            await _agentOrchestrator.RegisterAgentAsync(agent1);
            await _agentOrchestrator.RegisterAgentAsync(agent2);
            
            // Act
            var result = await _example.DemonstrateAgentCommunicationAsync();
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Agent communication successful", result.Message);
        }
        
        [Fact]
        public async Task DemonstrateAgentCommunicationAsync_InsufficientAgents_ShouldFail()
        {
            // Act - No agents registered
            var result = await _example.DemonstrateAgentCommunicationAsync();
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("At least 2 agents required", result.Message);
        }
        
        [Fact]
        public async Task AgentComposableCommand_ShouldImplementInterface()
        {
            // Arrange
            var agent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            var context = new AgentContext(agent.Platform);
            var factory = new AgentComposableCommandFactory();
            
            // Act
            var command = await factory.CreateAgentCommandAsync(agent, task, context);
            
            // Assert
            Assert.NotNull(command);
            Assert.IsAssignableFrom<IAgentComposableCommand>(command);
            Assert.Equal(agent, command.Agent);
            Assert.Equal(task, command.AgentTask);
            Assert.Equal(context, command.AgentContext);
        }
        
        [Fact]
        public async Task AgentComposableCommand_ExecuteAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            var context = new AgentContext(agent.Platform);
            var factory = new AgentComposableCommandFactory();
            var command = await factory.CreateAgentCommandAsync(agent, task, context);
            
            var commandContext = CommandContext.Create(command.Id, ("test", "value"));
            
            // Act
            var result = await command.ExecuteAsync(commandContext);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Agent command executed successfully", result.Message);
        }
        
        [Fact]
        public async Task AgentComposableCommand_ValidateAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            var context = new AgentContext(agent.Platform);
            var factory = new AgentComposableCommandFactory();
            var command = await factory.CreateAgentCommandAsync(agent, task, context);
            
            var commandContext = CommandContext.Create(command.Id);
            
            // Act
            var result = await command.ValidateAsync(commandContext);
            
            // Assert
            Assert.True(result.IsValid);
        }
        
        [Fact]
        public async Task AgentComposableCommand_RollbackAsync_ShouldSucceed()
        {
            // Arrange
            var agent = new TestCodeGenerationAgent(
                new AgentId(Guid.NewGuid().ToString()),
                new AgentName("Test Agent"),
                PlatformType.CrossPlatform);
            
            var task = new AgentTask(
                new TaskId(Guid.NewGuid().ToString()),
                new TaskName("Test Task"),
                new TaskDescription("Test task"),
                TaskType.General,
                TaskPriority.Medium,
                new Dictionary<string, object>());
            
            var context = new AgentContext(agent.Platform);
            var factory = new AgentComposableCommandFactory();
            var command = await factory.CreateAgentCommandAsync(agent, task, context);
            
            var commandContext = CommandContext.Create(command.Id);
            
            // Act
            var result = await command.RollbackAsync(commandContext);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("Agent rollback completed successfully", result.Message);
        }
    }
}
