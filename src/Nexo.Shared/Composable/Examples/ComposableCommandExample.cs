using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Examples
{
    /// <summary>
    /// Example demonstrating the composable command system
    /// </summary>
    public class ComposableCommandExample
    {
        private readonly IComposableOrchestrator _orchestrator;
        private readonly ComposableCommandFactory _factory;
        
        public ComposableCommandExample(
            IComposableOrchestrator? orchestrator = null,
            ComposableCommandFactory? factory = null)
        {
            _orchestrator = orchestrator ?? new ComposableOrchestrator();
            _factory = factory ?? new ComposableCommandFactory();
        }
        
        /// <summary>
        /// Demonstrates creating and executing a composable command
        /// </summary>
        public async Task<ICommandResult> DemonstrateBasicCommandAsync()
        {
            // Create command
            var command = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            
            // Register command
            await _orchestrator.RegisterCommandAsync(command);
            
            // Create context
            var context = CommandContext.Create(
                command.Id,
                ("input", "Hello Composable World!"),
                ("metadata", "example-demo"));
            
            // Execute command
            var result = await _orchestrator.ExecuteCommandAsync(command.Id, context);
            
            return result;
        }
        
        /// <summary>
        /// Demonstrates executing multiple commands in sequence
        /// </summary>
        public async Task<IReadOnlyList<ICommandResult>> DemonstrateSequenceExecutionAsync()
        {
            // Create multiple commands
            var command1 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            var command2 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            
            // Register commands
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            
            // Create context
            var context = CommandContext.Create(
                command1.Id,
                ("input", "Sequence Execution"),
                ("metadata", "sequence-demo"));
            
            // Execute commands in sequence
            var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id };
            var results = await _orchestrator.ExecuteSequenceAsync(commandIds, context);
            
            return results;
        }
        
        /// <summary>
        /// Demonstrates executing multiple commands in parallel
        /// </summary>
        public async Task<IReadOnlyList<ICommandResult>> DemonstrateParallelExecutionAsync()
        {
            // Create multiple commands
            var command1 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            var command2 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            var command3 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            
            // Register commands
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            await _orchestrator.RegisterCommandAsync(command3);
            
            // Create context
            var context = CommandContext.Create(
                command1.Id,
                ("input", "Parallel Execution"),
                ("metadata", "parallel-demo"));
            
            // Execute commands in parallel
            var commandIds = new List<ICommandIdentifier> { command1.Id, command2.Id, command3.Id };
            var results = await _orchestrator.ExecuteParallelAsync(commandIds, context);
            
            return results;
        }
        
        /// <summary>
        /// Demonstrates workflow execution
        /// </summary>
        public async Task<IWorkflowResult> DemonstrateWorkflowExecutionAsync()
        {
            // Create commands
            var command1 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            var command2 = await _factory.CreateCommandAsync<ExampleComposableCommand>();
            
            // Register commands
            await _orchestrator.RegisterCommandAsync(command1);
            await _orchestrator.RegisterCommandAsync(command2);
            
            // Create workflow definition
            var workflow = new WorkflowDefinition(
                "Example Workflow",
                "A simple workflow demonstrating composable commands",
                "This workflow demonstrates how to chain multiple composable commands together",
                new List<IWorkflowStep>
                {
                    new WorkflowStep(
                        "Step_1",
                        "Execute Command 1",
                        "Execute the first command in the workflow",
                        "Command",
                        "Pending",
                        1),
                    new WorkflowStep(
                        "Step_2",
                        "Execute Command 2",
                        "Execute the second command in the workflow",
                        "Command",
                        "Pending",
                        2)
                });
            
            // Create context
            var context = CommandContext.Create(
                command1.Id,
                ("input", "Workflow Execution"),
                ("metadata", "workflow-demo"));
            
            // Execute workflow
            var result = await _orchestrator.ExecuteWorkflowAsync(workflow, context);
            
            return result;
        }
        
        /// <summary>
        /// Demonstrates dependency injection
        /// </summary>
        public async Task<ICommandResult> DemonstrateDependencyInjectionAsync()
        {
            // Create custom operations
            var customCollectionOps = new CollectionOperations();
            var customLoopOps = new LoopOperations();
            var customStringOps = new StringOperations();
            
            // Create command with custom dependencies
            var command = await _factory.CreateCommandWithDependenciesAsync<ExampleComposableCommand>(
                customCollectionOps,
                customLoopOps,
                customStringOps);
            
            // Register command
            await _orchestrator.RegisterCommandAsync(command);
            
            // Create context
            var context = CommandContext.Create(
                command.Id,
                ("input", "Dependency Injection Demo"),
                ("metadata", "di-demo"));
            
            // Execute command
            var result = await _orchestrator.ExecuteCommandAsync(command.Id, context);
            
            return result;
        }
    }
    
    
}
