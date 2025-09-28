using System;
using System.Collections.Generic;

namespace Nexo.Shared.Composable.CommandTypes
{
    /// <summary>
    /// Interface for command dependencies
    /// </summary>
    public interface ICommandDependencies
    {
        IReadOnlyList<ICommandIdentifier> RequiredCommands { get; }
        IReadOnlyList<ICommandIdentifier> OptionalCommands { get; }
        IReadOnlyList<ICommandIdentifier> ConflictingCommands { get; }
        IReadOnlyDictionary<string, object> RequiredParameters { get; }
        IReadOnlyDictionary<string, object> OptionalParameters { get; }
    }
    
    /// <summary>
    /// Default implementation of command dependencies
    /// </summary>
    public class CommandDependencies : ICommandDependencies
    {
        public IReadOnlyList<ICommandIdentifier> RequiredCommands { get; }
        public IReadOnlyList<ICommandIdentifier> OptionalCommands { get; }
        public IReadOnlyList<ICommandIdentifier> ConflictingCommands { get; }
        public IReadOnlyDictionary<string, object> RequiredParameters { get; }
        public IReadOnlyDictionary<string, object> OptionalParameters { get; }
        
        public CommandDependencies(
            IReadOnlyList<ICommandIdentifier>? requiredCommands = null,
            IReadOnlyList<ICommandIdentifier>? optionalCommands = null,
            IReadOnlyList<ICommandIdentifier>? conflictingCommands = null,
            IReadOnlyDictionary<string, object>? requiredParameters = null,
            IReadOnlyDictionary<string, object>? optionalParameters = null)
        {
            RequiredCommands = requiredCommands ?? new List<ICommandIdentifier>();
            OptionalCommands = optionalCommands ?? new List<ICommandIdentifier>();
            ConflictingCommands = conflictingCommands ?? new List<ICommandIdentifier>();
            RequiredParameters = requiredParameters ?? new Dictionary<string, object>();
            OptionalParameters = optionalParameters ?? new Dictionary<string, object>();
        }
        
        public static ICommandDependencies None() => new CommandDependencies();
        
        public static ICommandDependencies Require(params ICommandIdentifier[] commands) => 
            new CommandDependencies(requiredCommands: commands);
            
        public static ICommandDependencies Optional(params ICommandIdentifier[] commands) => 
            new CommandDependencies(optionalCommands: commands);
            
        public static ICommandDependencies Conflicting(params ICommandIdentifier[] commands) => 
            new CommandDependencies(conflictingCommands: commands);
    }
}
