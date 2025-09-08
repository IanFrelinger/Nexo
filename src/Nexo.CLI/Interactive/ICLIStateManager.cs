using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nexo.CLI.Interactive
{
    /// <summary>
    /// Interface for managing persistent CLI state including user preferences and command history
    /// </summary>
    public interface ICLIStateManager
    {
        /// <summary>
        /// Gets the current CLI context including project, platform, and user preferences
        /// </summary>
        Task<CLIContext> GetCurrentContextAsync();
        
        /// <summary>
        /// Sets the current project context
        /// </summary>
        Task SetCurrentProjectAsync(ProjectInfo project);
        
        /// <summary>
        /// Sets the current platform context
        /// </summary>
        Task SetCurrentPlatformAsync(string platform);
        
        /// <summary>
        /// Adds a command to the history
        /// </summary>
        Task AddToHistoryAsync(string command);
        
        /// <summary>
        /// Gets the command history
        /// </summary>
        Task<List<string>> GetCommandHistoryAsync();
        
        /// <summary>
        /// Updates a user preference
        /// </summary>
        Task UpdateUserPreferenceAsync(string key, object value);
        
        /// <summary>
        /// Gets a user preference
        /// </summary>
        Task<T?> GetUserPreferenceAsync<T>(string key);
        
        /// <summary>
        /// Clears the command history
        /// </summary>
        Task ClearHistoryAsync();
        
        /// <summary>
        /// Saves the current state to persistent storage
        /// </summary>
        Task SaveStateAsync();
    }
    
    /// <summary>
    /// Represents a command history entry
    /// </summary>
    public class CommandHistoryEntry
    {
        public string Command { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string WorkingDirectory { get; set; } = string.Empty;
        public bool Successful { get; set; } = true;
        public TimeSpan ExecutionTime { get; set; }
    }
}
