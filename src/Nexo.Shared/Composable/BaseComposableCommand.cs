using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nexo.Shared.Composable.CommandTypes;
using Nexo.Shared.Operations;

namespace Nexo.Shared.Composable
{
    /// <summary>
    /// Base implementation of composable command with dependency injection
    /// </summary>
    public abstract class BaseComposableCommand : IComposableCommand
    {
        protected readonly ICollectionOperations _collectionOps;
        protected readonly ILoopOperations _loopOps;
        protected readonly IStringOperations _stringOps;
        
        public abstract ICommandIdentifier Id { get; }
        public abstract ICommandName Name { get; }
        public abstract ICommandDescription Description { get; }
        public abstract ICommandStatus Status { get; }
        public abstract ICommandDependencies Dependencies { get; }
        public abstract ICommandCapabilities Capabilities { get; }
        
        protected BaseComposableCommand(
            ICollectionOperations? collectionOps = null,
            ILoopOperations? loopOps = null,
            IStringOperations? stringOps = null)
        {
            _collectionOps = collectionOps ?? new CollectionOperations();
            _loopOps = loopOps ?? new LoopOperations();
            _stringOps = stringOps ?? new StringOperations();
        }
        
        public abstract Task<ICommandResult> ExecuteAsync(ICommandContext context);
        
        public virtual async Task<IValidationResult> ValidateAsync(ICommandContext context)
        {
            // Default validation - can be overridden
            var errors = new List<IValidationError>();
            var warnings = new List<IValidationWarning>();
            
            // Validate required parameters
            foreach (var (key, value) in context.Parameters)
            {
                if (value == null)
                {
                    errors.Add(new ValidationError("REQUIRED_PARAMETER", $"Parameter '{key}' is required", key, "Error"));
                }
            }
            
            return new ValidationResult(errors.Count == 0, errors, warnings);
        }
        
        public virtual async Task<ICommandResult> RollbackAsync(ICommandContext context)
        {
            // Default rollback - can be overridden
            return CommandResult.Success(Id, "Rollback completed successfully");
        }
        
        /// <summary>
        /// Helper method to create success result
        /// </summary>
        protected ICommandResult CreateSuccessResult(string? message = null, object? data = null, IReadOnlyDictionary<string, object>? metadata = null)
        {
            return CommandResult.Success(Id, message, data, metadata);
        }
        
        /// <summary>
        /// Helper method to create failure result
        /// </summary>
        protected ICommandResult CreateFailureResult(string message, IReadOnlyList<IValidationError>? errors = null)
        {
            return CommandResult.Failure(Id, message, errors);
        }
        
        /// <summary>
        /// Helper method to create validation error
        /// </summary>
        protected IValidationError CreateValidationError(string code, string message, string? property = null, string severity = "Error")
        {
            return new ValidationError(code, message, property, severity);
        }
        
        /// <summary>
        /// Helper method to create validation warning
        /// </summary>
        protected IValidationWarning CreateValidationWarning(string code, string message, string? property = null)
        {
            return new ValidationWarning(code, message, property);
        }
    }
}
