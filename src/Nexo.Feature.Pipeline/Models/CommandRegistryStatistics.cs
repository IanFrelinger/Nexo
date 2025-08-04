using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Statistics about the command registry.
    /// </summary>
    public class CommandRegistryStatistics
{
    /// <summary>
    /// Total number of registered commands.
    /// </summary>
    public int TotalCommands { get; set; }
    
    /// <summary>
    /// Number of commands by category.
    /// </summary>
    public Dictionary<string, int> CommandsByCategory { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Number of commands by priority.
    /// </summary>
    public Dictionary<string, int> CommandsByPriority { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Number of commands that can be executed in parallel.
    /// </summary>
    public int ParallelExecutableCommands { get; set; }
    
    /// <summary>
    /// Number of commands with dependencies.
    /// </summary>
    public int CommandsWithDependencies { get; set; }
    
    /// <summary>
    /// Number of commands with tags.
    /// </summary>
    public int CommandsWithTags { get; set; }
    
    /// <summary>
    /// Most common tags.
    /// </summary>
    public Dictionary<string, int> MostCommonTags { get; set; } = new Dictionary<string, int>();
    
    /// <summary>
    /// Timestamp when statistics were generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
}