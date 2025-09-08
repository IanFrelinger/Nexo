using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Interface for intelligent command suggestion engine with context awareness
    /// </summary>
    public interface ICommandSuggestionEngine
    {
        /// <summary>
        /// Gets command completions for tab completion
        /// </summary>
        Task<IEnumerable<string>> GetCompletionsAsync(string partialInput);
        
        /// <summary>
        /// Gets contextual command suggestions based on current CLI state
        /// </summary>
        Task<IEnumerable<CommandSuggestion>> GetContextualSuggestionsAsync(CLIContext context);
        
        /// <summary>
        /// Gets AI-powered suggestions based on user behavior and context
        /// </summary>
        Task<IEnumerable<CommandSuggestion>> GetAIPoweredSuggestionsAsync(CLIContext context);
        
        /// <summary>
        /// Gets suggestions based on recent user activity
        /// </summary>
        Task<IEnumerable<CommandSuggestion>> GetRecentActivitySuggestionsAsync(CLIContext context);
    }
    
    /// <summary>
    /// Represents a command suggestion with relevance scoring
    /// </summary>
    public class CommandSuggestion
    {
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents the current CLI context for intelligent suggestions
    /// </summary>
    public class CLIContext
    {
        public ProjectInfo? CurrentProject { get; set; }
        public string? CurrentPlatform { get; set; }
        public List<string> RecentCommands { get; set; } = new();
        public Dictionary<string, object> UserPreferences { get; set; } = new();
        public bool HasActiveMonitoring { get; set; }
        public bool HasPendingAdaptations { get; set; }
        public bool HasPerformanceIssues { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Represents project information for context
    /// </summary>
    public class ProjectInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Framework { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}
