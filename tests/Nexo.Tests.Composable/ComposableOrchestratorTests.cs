using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Nexo.Shared.Composable;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Tests.Composable
{
    /// <summary>
    /// Tests for the composable command orchestrator
    /// </summary>
    public class ComposableOrchestratorTests
    {
        private readonly IComposableOrchestrator _orchestrator;
        private readonly ICollectionOperations _collectionOps;
        private readonly ILoopOperations _loopOps;
        
        public ComposableOrchestratorTests()
        {
            _collectionOps = new CollectionOperations();
            _loopOps = new LoopOperations();
            _orchestrator = new ComposableOrchestrator(_collectionOps, _loopOps);
        }
        
        [Fact]
        public async Task RegisterCommandAsync_ShouldSucceed()
        {
            // Arrange
            var command = new TestComposableCommand();
            
            // Act
            var result = await _orchestrator.RegisterCommandAsync(command);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Command registered successfully", result.Message);
        }
        
        [Fact]
        public async Task RegisterCommandAsync_WithNullCommand_ShouldFail()
        {
            // Act
            var result = await _orchestrator.RegisterCommandAsync(null);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Command cannot be null", result.Message);
        }
        
        [Fact]
        public async Task RegisterCommandAsync_DuplicateCommand_ShouldFail()
        {
            // Arrange
            var command1 = new TestComposableCommand();
            var command2 = new TestComposableCommand();
            
            // Act
            await _orchestrator.RegisterCommandAsync(command1);
            var result = await _orchestrator.RegisterCommandAsync(command2);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Command is already registered", result.Message);
        }
        
        [Fact]
        public async Task UnregisterCommandAsync_ShouldSucceed()
        {
            // Arrange
            var command = new TestComposableCommand();
            await _orchestrator.RegisterCommandAsync(command);
            
            // Act
            var result = await _orchestrator.UnregisterCommandAsync(command.Id);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Command unregistered successfully", result.Message);
        }
        
        [Fact]
        public async Task UnregisterCommandAsync_NonExistentCommand_ShouldFail()
        {
            // Arrange
            var commandId = new CommandIdentifier("NonExistent", "Test", "1.0.0");
            
            // Act
            var result = await _orchestrator.UnregisterCommandAsync(commandId);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Command is not registered", result.Message);
        }
        
        [Fact]
        public async Task ExecuteCommandAsync_ShouldSucceed()
        {
            // Arrange
            var command = new TestComposableCommand();
            await _orchestrator.RegisterCommandAsync(command);
            var context = CommandContext.Create(command.Id, ("input", "test"));
            
            // Act
            var result = await _orchestrator.ExecuteCommandAsync(command.Id, context);
            
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Test command executed successfully", result.Message);
        }
        
        [Fact]
        public async Task ExecuteCommandAsync_NonExistentCommand_ShouldFail()
        {
            // Arrange
            var commandId = new CommandIdentifier("NonExistent", "Test", "1.0.0");
            var context = CommandContext.Create(commandId);
            
            // Act
            var result = await _orchestrator.ExecuteCommandAsync(commandId, context);
            
            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Command is not registered", result.Message);
        }
        
        [Fact]
        public async Task ExecuteSequenceAsync_ShouldExecuteInOrder()
        {
            // Arrange
            var command1 = new TestComposableCommand("Command1");
            var command2 = new TestComposableCommand("Command2");
            var command3 = new TestComposableCommand("Command3");
            
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            await _orchestrator.RegisterCommandAsync(command3);
            
            var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id, command3.Id };
            var context = CommandContext.Create(command1.Id);
            
            // Act
            var results = await _orchestrator.ExecuteSequenceAsync(commandIds, context);
            
            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
        
        [Fact]
        public async Task ExecuteParallelAsync_ShouldExecuteInParallel()
        {
            // Arrange
            var command1 = new TestComposableCommand("Command1");
            var command2 = new TestComposableCommand("Command2");
            var command3 = new TestComposableCommand("Command3");
            
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            await _orchestrator.RegisterCommandAsync(command3);
            
            var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id, command3.Id };
            var context = CommandContext.Create(command1.Id);
            
            // Act
            var results = await _orchestrator.ExecuteParallelAsync(commandIds, context);
            
            // Assert
            Assert.Equal(3, results.Count);
            Assert.All(results, result => Assert.True(result.IsSuccess));
        }
        
        [Fact]
        public async Task GetRegisteredCommandsAsync_ShouldReturnAllCommands()
        {
            // Arrange
            var command1 = new TestComposableCommand("Command1");
            var command2 = new TestComposableCommand("Command2");
            
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            
            // Act
            var commands = await _orchestrator.GetRegisteredCommandsAsync();
            
            // Assert
            Assert.Equal(2, commands.Count);
        }
        
        [Fact]
        public async Task GetCommandAsync_ShouldReturnSpecificCommand()
        {
            // Arrange
            var command = new TestComposableCommand("TestCommand");
            await _orchestrator.RegisterCommandAsync(command);
            
            // Act
            var retrievedCommand = await _orchestrator.GetCommandAsync(command.Id);
            
            // Assert
            Assert.NotNull(retrievedCommand);
            Assert.Equal(command.Id, retrievedCommand.Id);
        }
        
        [Fact]
        public async Task GetCommandAsync_NonExistentCommand_ShouldReturnNull()
        {
            // Arrange
            var commandId = new CommandIdentifier("NonExistent", "Test", "1.0.0");
            
            // Act
            var command = await _orchestrator.GetCommandAsync(commandId);
            
            // Assert
            Assert.Null(command);
        }
        
        [Fact]
        public async Task ValidateExecutionPlanAsync_ShouldValidateDependencies()
        {
            // Arrange
            var command1 = new TestComposableCommand("Command1");
            var command2 = new TestComposableCommand("Command2");
            
            await _orchestrator.RegisterCommandAsync(command1);
            // Don't register command2 to test missing dependency
            
            var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id };
            var context = CommandContext.Create(command1.Id);
            
            // Act
            var validationResult = await _orchestrator.ValidateExecutionPlanAsync(commandIds, context);
            
            // Assert
            Assert.False(validationResult.IsValid);
            Assert.True(validationResult.Errors.Count > 0);
        }
    }
    
    /// <summary>
    /// Test implementation of composable command
    /// </summary>
    public class TestComposableCommand : BaseComposableCommand
    {
        private readonly string _name;
        
        public TestComposableCommand(string name = "TestCommand")
        {
            _name = name;
        }
        
        public override ICommandIdentifier Id => new CommandIdentifier(_name, "Test", "1.0.0");
        public override ICommandName Name => new CommandName(_name, _name, "Test");
        public override ICommandDescription Description => new CommandDescription($"Test command: {_name}");
        public override ICommandStatus Status => CommandStatus.Ready();
        public override ICommandDependencies Dependencies => CommandDependencies.None();
        public override ICommandCapabilities Capabilities => CommandCapabilities.None();
        
        public override async Task<ICommandResult> ExecuteAsync(ICommandContext context)
        {
            await Task.CompletedTask;
            return CreateSuccessResult("Test command executed successfully", new { CommandName = _name });
        }
    }
}
