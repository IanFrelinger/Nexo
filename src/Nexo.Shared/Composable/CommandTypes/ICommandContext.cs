using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command execution context
    /// </summary>
    public interface ICommandContext
    {
        ICommandIdentifier CommandId { get; }
        IReadOnlyDictionary<string, object> Parameters { get; }
        IReadOnlyDictionary<string, object> Metadata { get; }
        IReadOnlyList<ICommandResult> PreviousResults { get; }
        IReadOnlyList<ICommandResult> DependentResults { get; }
        DateTime StartedAt { get; }
        string CorrelationId { get; }
        ICommandContext? ParentContext { get; }
    }
    
    /// <summary>
    /// Default implementation of command context
    /// </summary>
    public class CommandContext : ICommandContext
    {
        public ICommandIdentifier CommandId { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }
        public IReadOnlyList<ICommandResult> PreviousResults { get; }
        public IReadOnlyList<ICommandResult> DependentResults { get; }
        public DateTime StartedAt { get; }
        public string CorrelationId { get; }
        public ICommandContext? ParentContext { get; }
        
        public CommandContext(
            ICommandIdentifier commandId,
            IReadOnlyDictionary<string, object>? parameters = null,
            IReadOnlyDictionary<string, object>? metadata = null,
            IReadOnlyList<ICommandResult>? previousResults = null,
            IReadOnlyList<ICommandResult>? dependentResults = null,
            string? correlationId = null,
            ICommandContext? parentContext = null)
        {
            CommandId = commandId ?? throw new ArgumentNullException(nameof(commandId));
            Parameters = parameters ?? new Dictionary<string, object>();
            Metadata = metadata ?? new Dictionary<string, object>();
            PreviousResults = previousResults ?? new List<ICommandResult>();
            DependentResults = dependentResults ?? new List<ICommandResult>();
            StartedAt = DateTime.UtcNow;
            CorrelationId = correlationId ?? Guid.NewGuid().ToString();
            ParentContext = parentContext;
        }
        
        public static ICommandContext Create(ICommandIdentifier commandId, params (string key, object value)[] parameters)
        {
            var paramDict = new Dictionary<string, object>();
            foreach (var (key, value) in parameters)
            {
                paramDict[key] = value;
            }
            return new CommandContext(commandId, paramDict);
        }
    }
}
