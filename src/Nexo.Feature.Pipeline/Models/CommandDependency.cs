using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Represents a dependency between commands.
    /// </summary>
    public class CommandDependency
{
    /// <summary>
    /// ID of the command that this command depends on.
    /// </summary>
    public string DependentCommandId { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of dependency.
    /// </summary>
    public DependencyType Type { get; set; }
    
    /// <summary>
    /// Whether the dependency is required.
    /// </summary>
    public bool IsRequired { get; set; } = true;
    
    /// <summary>
    /// Condition that must be met for the dependency to be satisfied.
    /// </summary>
    public string? Condition { get; set; }
    
    /// <summary>
    /// Additional metadata about the dependency.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    
    public CommandDependency(string dependentCommandId, DependencyType type = DependencyType.Execution, bool isRequired = true)
    {
        DependentCommandId = dependentCommandId;
        Type = type;
        IsRequired = isRequired;
    }
    
    /// <summary>
    /// Creates a required execution dependency.
    /// </summary>
    /// <param name="dependentCommandId">ID of the dependent command.</param>
    /// <returns>A required execution dependency.</returns>
    public static CommandDependency Required(string dependentCommandId)
    {
        return new CommandDependency(dependentCommandId, DependencyType.Execution, true);
    }
    
    /// <summary>
    /// Creates an optional execution dependency.
    /// </summary>
    /// <param name="dependentCommandId">ID of the dependent command.</param>
    /// <returns>An optional execution dependency.</returns>
    public static CommandDependency Optional(string dependentCommandId)
    {
        return new CommandDependency(dependentCommandId, DependencyType.Execution, false);
    }
    
    /// <summary>
    /// Creates a data dependency.
    /// </summary>
    /// <param name="dependentCommandId">ID of the dependent command.</param>
    /// <param name="isRequired">Whether the dependency is required.</param>
    /// <returns>A data dependency.</returns>
    public static CommandDependency Data(string dependentCommandId, bool isRequired = true)
    {
        return new CommandDependency(dependentCommandId, DependencyType.Data, isRequired);
    }
    
    /// <summary>
    /// Creates a resource dependency.
    /// </summary>
    /// <param name="dependentCommandId">ID of the dependent command.</param>
    /// <param name="isRequired">Whether the dependency is required.</param>
    /// <returns>A resource dependency.</returns>
    public static CommandDependency Resource(string dependentCommandId, bool isRequired = true)
    {
        return new CommandDependency(dependentCommandId, DependencyType.Resource, isRequired);
    }
}

/// <summary>
/// Type of dependency between commands.
/// </summary>
public enum DependencyType
{
    /// <summary>
    /// Execution dependency - the dependent command must execute before this command.
    /// </summary>
    Execution,
    
    /// <summary>
    /// Data dependency - this command depends on data produced by the dependent command.
    /// </summary>
    Data,
    
    /// <summary>
    /// Resource dependency - this command depends on resources managed by the dependent command.
    /// </summary>
    Resource,
    
    /// <summary>
    /// Conditional dependency - this command depends on the dependent command under certain conditions.
    /// </summary>
    Conditional
}
}