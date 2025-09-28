using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable.Examples
{
    /// <summary>
    /// Example composable command that demonstrates the pattern
    /// </summary>
    public class ExampleComposableCommand : BaseComposableCommand
    {
        public override ICommandIdentifier Id { get; }
        public override ICommandName Name { get; }
        public override ICommandDescription Description { get; }
        public override ICommandStatus Status { get; }
        public override ICommandDependencies Dependencies { get; }
        public override ICommandCapabilities Capabilities { get; }
        
        public ExampleComposableCommand(
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null) 
            : base(collectionOps, loopOps, stringOps)
        {
            Id = new CommandIdentifier("ExampleCommand", "Nexo.Examples", "1.0.0");
            Name = new CommandName("ExampleCommand", "Example Command", "Examples");
            Description = new CommandDescription(
                "An example command that demonstrates the composable pattern",
                "This command shows how to create self-contained, composable commands with dependency injection",
                new[] { "example", "demo", "composable" },
                new[] { "ExampleCommand --input 'Hello World'" });
            Status = CommandStatus.Ready();
            Dependencies = CommandDependencies.None();
            Capabilities = CommandCapabilities.None();
        }
        
        public override async Task<ICommandResult> ExecuteAsync(ICommandContext context)
        {
            try
            {
                // Get input parameter
                var input = context.Parameters.TryGetValue("input", out var inputValue) ? inputValue?.ToString() : "Default Input";
                
                // Use dependency-wrapped string operations
                var processedInput = await _stringOps.ToUpperAsync(input);
                var trimmedInput = await _stringOps.TrimAsync(processedInput);
                
                // Create result data
                var resultData = new
                {
                    OriginalInput = input,
                    ProcessedInput = trimmedInput,
                    ProcessedAt = DateTime.UtcNow,
                    CommandId = Id.ToString()
                };
                
                // Create metadata
                var metadata = new Dictionary<string, object>
                {
                    ["InputLength"] = input?.Length ?? 0,
                    ["ProcessedLength"] = trimmedInput?.Length ?? 0,
                    ["ProcessingTime"] = DateTime.UtcNow - context.StartedAt
                };
                
                return CreateSuccessResult("Example command executed successfully", resultData, metadata);
            }
            catch (Exception ex)
            {
                return CreateFailureResult($"Example command failed: {ex.Message}");
            }
        }
        
        public override async Task<IValidationResult> ValidateAsync(ICommandContext context)
        {
            var errors = new List<IValidationError>();
            var warnings = new List<IValidationWarning>();
            
            // Validate input parameter
            if (!context.Parameters.ContainsKey("input"))
            {
                warnings.Add(CreateValidationWarning("MISSING_INPUT", "Input parameter is missing, using default value"));
            }
            else
            {
                var input = context.Parameters["input"]?.ToString();
                if (await _stringOps.IsNullOrEmptyAsync(input))
                {
                    warnings.Add(CreateValidationWarning("EMPTY_INPUT", "Input parameter is empty"));
                }
            }
            
            return new ValidationResult(errors.Count == 0, errors, warnings);
        }
    }
}
