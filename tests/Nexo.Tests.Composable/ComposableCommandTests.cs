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
    /// Tests for the composable command system
    /// </summary>
    public class ComposableCommandTests
    {
        [Fact]
        public async Task CreateCommandIdentifier_ShouldSucceed()
        {
            // Arrange
            var id = new CommandIdentifier("TestCommand", "TestNamespace", "1.0.0");
            
            // Act & Assert
            Assert.Equal("TestCommand", id.Value);
            Assert.Equal("TestNamespace", id.Namespace);
            Assert.Equal("1.0.0", id.Version);
            Assert.True(id.CreatedAt <= DateTime.UtcNow);
        }
        
        [Fact]
        public async Task CreateCommandName_ShouldSucceed()
        {
            // Arrange
            var name = new CommandName("TestCommand", "Test Display Name", "TestCategory");
            
            // Act & Assert
            Assert.Equal("TestCommand", name.Value);
            Assert.Equal("Test Display Name", name.DisplayName);
            Assert.Equal("TestCategory", name.Category);
        }
        
        [Fact]
        public async Task CreateCommandDescription_ShouldSucceed()
        {
            // Arrange
            var description = new CommandDescription(
                "Test Summary",
                "Detailed description",
                new[] { "tag1", "tag2" },
                new[] { "example1", "example2" });
            
            // Act & Assert
            Assert.Equal("Test Summary", description.Summary);
            Assert.Equal("Detailed description", description.DetailedDescription);
            Assert.Equal(2, description.Tags.Length);
            Assert.Equal(2, description.Examples.Length);
        }
        
        [Fact]
        public async Task CreateCommandStatus_ShouldSucceed()
        {
            // Arrange
            var status = CommandStatus.Ready("Test message");
            
            // Act & Assert
            Assert.Equal("Ready", status.State);
            Assert.Equal("Test message", status.Message);
            Assert.True(status.IsExecutable);
            Assert.False(status.IsCompleted);
            Assert.False(status.IsFailed);
        }
        
        [Fact]
        public async Task CreateCommandDependencies_ShouldSucceed()
        {
            // Arrange
            var commandId1 = new CommandIdentifier("Command1", "Test", "1.0.0");
            var commandId2 = new CommandIdentifier("Command2", "Test", "1.0.0");
            var dependencies = CommandDependencies.Require(commandId1, commandId2);
            
            // Act & Assert
            Assert.Equal(2, dependencies.RequiredCommands.Count);
            Assert.Equal(0, dependencies.OptionalCommands.Count);
            Assert.Equal(0, dependencies.ConflictingCommands.Count);
        }
        
        [Fact]
        public async Task CreateCommandCapabilities_ShouldSucceed()
        {
            // Arrange
            var capability1 = new Capability("TestCapability1", "Test Description 1", "TestCategory", 5);
            var capability2 = new Capability("TestCapability2", "Test Description 2", "TestCategory", 8);
            var capabilities = CommandCapabilities.WithCapabilities(capability1, capability2);
            
            // Act & Assert
            Assert.Equal(2, capabilities.ProvidedCapabilities.Count);
            Assert.Equal(0, capabilities.RequiredCapabilities.Count);
        }
        
        [Fact]
        public async Task CreateCommandContext_ShouldSucceed()
        {
            // Arrange
            var commandId = new CommandIdentifier("TestCommand", "Test", "1.0.0");
            var parameters = new Dictionary<string, object> { ["param1"] = "value1" };
            var metadata = new Dictionary<string, object> { ["meta1"] = "metaValue1" };
            
            // Act
            var context = new CommandContext(commandId, parameters, metadata);
            
            // Assert
            Assert.Equal(commandId, context.CommandId);
            Assert.Equal(parameters, context.Parameters);
            Assert.Equal(metadata, context.Metadata);
            Assert.NotNull(context.CorrelationId);
        }
        
        [Fact]
        public async Task CreateCommandResult_ShouldSucceed()
        {
            // Arrange
            var commandId = new CommandIdentifier("TestCommand", "Test", "1.0.0");
            var data = new { TestProperty = "TestValue" };
            
            // Act
            var successResult = CommandResult.Success(commandId, "Test message", data);
            var failureResult = CommandResult.Failure(commandId, "Test error");
            
            // Assert
            Assert.True(successResult.IsSuccess);
            Assert.Equal("Test message", successResult.Message);
            Assert.Equal(data, successResult.Data);
            
            Assert.False(failureResult.IsSuccess);
            Assert.Equal("Test error", failureResult.Message);
        }
        
        [Fact]
        public async Task CreateValidationResult_ShouldSucceed()
        {
            // Arrange
            var error = new ValidationError("TEST_ERROR", "Test error message", "TestProperty", "Error");
            var warning = new ValidationWarning("TEST_WARNING", "Test warning message", "TestProperty");
            
            // Act
            var validResult = ValidationResult.Valid();
            var invalidResult = ValidationResult.Invalid(error);
            var warningResult = ValidationResult.WithWarnings(warning);
            
            // Assert
            Assert.True(validResult.IsValid);
            Assert.Equal(0, validResult.Errors.Count);
            
            Assert.False(invalidResult.IsValid);
            Assert.Equal(1, invalidResult.Errors.Count);
            Assert.Equal("TEST_ERROR", invalidResult.Errors[0].Code);
            
            Assert.True(warningResult.IsValid);
            Assert.Equal(1, warningResult.Warnings.Count);
            Assert.Equal("TEST_WARNING", warningResult.Warnings[0].Code);
        }
    }
}
