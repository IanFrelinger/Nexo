using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command execution results
    /// </summary>
    public interface ICommandResult
    {
        ICommandIdentifier CommandId { get; }
        bool IsSuccess { get; }
        string Message { get; }
        string? ErrorMessage { get; }
        object? Data { get; }
        IReadOnlyDictionary<string, object>? Metadata { get; }
        IReadOnlyList<IValidationError>? Errors { get; }
        IReadOnlyList<IValidationWarning>? Warnings { get; }
        DateTime CompletedAt { get; }
        TimeSpan Duration { get; }
        string? CorrelationId { get; }
    }
    
    /// <summary>
    /// Interface for validation errors
    /// </summary>
    public interface IValidationError
    {
        string Code { get; }
        string Message { get; }
        string Property { get; }
        string Severity { get; }
    }
    
    /// <summary>
    /// Interface for validation warnings
    /// </summary>
    public interface IValidationWarning
    {
        string Code { get; }
        string Message { get; }
        string Property { get; }
    }
    
    /// <summary>
    /// Default implementation of command result
    /// </summary>
    public class CommandResult : ICommandResult
    {
        public ICommandIdentifier CommandId { get; }
        public bool IsSuccess { get; }
        public string Message { get; }
        public string? ErrorMessage { get; }
        public object? Data { get; }
        public IReadOnlyDictionary<string, object>? Metadata { get; }
        public IReadOnlyList<IValidationError>? Errors { get; }
        public IReadOnlyList<IValidationWarning>? Warnings { get; }
        public DateTime CompletedAt { get; }
        public TimeSpan Duration { get; }
        public string? CorrelationId { get; }
        
        public CommandResult(
            ICommandIdentifier commandId,
            bool isSuccess,
            string? message = null,
            string? errorMessage = null,
            object? data = null,
            IReadOnlyDictionary<string, object>? metadata = null,
            IReadOnlyList<IValidationError>? errors = null,
            IReadOnlyList<IValidationWarning>? warnings = null,
            TimeSpan? duration = null,
            string? correlationId = null)
        {
            CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            IsSuccess = isSuccess;
            Message = message ?? string.Empty;
            ErrorMessage = errorMessage;
            Data = data;
            Metadata = metadata;
            Errors = errors;
            Warnings = warnings;
            CompletedAt = DateTime.UtcNow;
            Duration = duration ?? TimeSpan.Zero;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
        }
        
        public static ICommandResult Success(
            ICommandIdentifier commandId, 
            string? message = "Command executed successfully", 
            object? data = null,
            IReadOnlyDictionary<string, object>? metadata = null,
            string? correlationId = null) =>
            new CommandResult(commandId, true, message, null, data, metadata, correlationId: correlationId);
            
        public static ICommandResult Failure(
            ICommandIdentifier commandId, 
            string? message = "Command execution failed", 
            IReadOnlyList<IValidationError>? errors = null,
            string? correlationId = null) =>
            new CommandResult(commandId, false, message, message, null, null, errors, correlationId: correlationId);
    }
}
