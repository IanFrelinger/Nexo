using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Interfaces;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Metadata about a command for discovery and documentation.
    /// </summary>
    public class CommandMetadata
{
    /// <summary>
    /// Unique identifier for this command.
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Human-readable name for this command.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of what this command does.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Category this command belongs to.
    /// </summary>
    public CommandCategory Category { get; set; }
    
    /// <summary>
    /// Tags for flexible organization and discovery.
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
    
    /// <summary>
    /// Priority level for execution ordering.
    /// </summary>
    public CommandPriority Priority { get; set; }
    
    /// <summary>
    /// Whether this command can be executed in parallel with other commands.
    /// </summary>
    public bool CanExecuteInParallel { get; set; }
    
    /// <summary>
    /// Dependencies that must be satisfied before this command can execute.
    /// </summary>
    public List<CommandDependency> Dependencies { get; set; } = new List<CommandDependency>();
    
    /// <summary>
    /// Parameters that this command accepts.
    /// </summary>
    public List<CommandParameter> Parameters { get; set; } = new List<CommandParameter>();
    
    /// <summary>
    /// Examples of how to use this command.
    /// </summary>
    public List<string> Examples { get; set; } = new List<string>();
    
    /// <summary>
    /// Version of this command.
    /// </summary>
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Author of this command.
    /// </summary>
    public string Author { get; set; } = string.Empty;
    
    /// <summary>
    /// Assembly that contains this command.
    /// </summary>
    public string AssemblyName { get; set; } = string.Empty;
    
    /// <summary>
    /// Type name of this command.
    /// </summary>
    public string TypeName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this command is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Whether this command is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; } = false;
    
    /// <summary>
    /// Deprecation message if this command is deprecated.
    /// </summary>
    public string? DeprecationMessage { get; set; }
    
    /// <summary>
    /// Additional metadata about this command.
    /// </summary>
    public Dictionary<string, object> AdditionalMetadata { get; set; } = new Dictionary<string, object>();
    
    /// <summary>
    /// Creates command metadata from a command.
    /// </summary>
    /// <param name="command">The command to create metadata for.</param>
    /// <returns>Command metadata.</returns>
    public static CommandMetadata FromCommand(ICommand command)
    {
        return new CommandMetadata
        {
            Id = command.Id,
            Name = command.Name,
            Description = command.Description,
            Category = command.Category,
            Tags = command.Tags.ToList(),
            Priority = command.Priority,
            CanExecuteInParallel = command.CanExecuteInParallel,
            Dependencies = command.Dependencies.ToList(),
            TypeName = command.GetType().FullName ?? command.GetType().Name,
            AssemblyName = command.GetType().Assembly.GetName().Name ?? string.Empty
        };
    }
}

/// <summary>
/// Represents a parameter that a command accepts.
/// </summary>
public class CommandParameter
{
    /// <summary>
    /// Name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the parameter.
    /// </summary>
    public Type ParameterType { get; set; } = typeof(object);
    
    /// <summary>
    /// Description of the parameter.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether the parameter is required.
    /// </summary>
    public bool IsRequired { get; set; }
    
    /// <summary>
    /// Default value for the parameter.
    /// </summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// Allowed values for the parameter (for enums or constrained parameters).
    /// </summary>
    public List<object> AllowedValues { get; set; } = new List<object>();
    
    /// <summary>
    /// Validation rules for the parameter.
    /// </summary>
    public List<string> ValidationRules { get; set; } = new List<string>();
    
    public CommandParameter(string name, Type parameterType, string description = "", bool isRequired = false)
    {
        Name = name;
        ParameterType = parameterType;
        Description = description;
        IsRequired = isRequired;
    }
}
}