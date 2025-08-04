using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.Feature.Pipeline.Interfaces
{
    /// <summary>
    /// Registry for managing command discovery, registration, and retrieval.
    /// Provides centralized command management and discovery capabilities.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// Registers a command with the registry.
        /// </summary>
        /// <param name="command">The command to register.</param>
        /// <returns>True if registration was successful, false if command with same ID already exists.</returns>
        bool RegisterCommand(ICommand command);
        
        /// <summary>
        /// Unregisters a command from the registry.
        /// </summary>
        /// <param name="commandId">The ID of the command to unregister.</param>
        /// <returns>True if the command was found and unregistered, false otherwise.</returns>
        bool UnregisterCommand(string commandId);
        
        /// <summary>
        /// Gets a command by its ID.
        /// </summary>
        /// <param name="commandId">The ID of the command to retrieve.</param>
        /// <returns>The command if found, null otherwise.</returns>
        ICommand GetCommand(string commandId);
        
        /// <summary>
        /// Gets all registered commands.
        /// </summary>
        /// <returns>All registered commands.</returns>
        IReadOnlyList<ICommand> GetAllCommands();
        
        /// <summary>
        /// Gets commands by category.
        /// </summary>
        /// <param name="category">The category to filter by.</param>
        /// <returns>Commands in the specified category.</returns>
        IReadOnlyList<ICommand> GetCommandsByCategory(CommandCategory category);
        
        /// <summary>
        /// Gets commands by tag.
        /// </summary>
        /// <param name="tag">The tag to filter by.</param>
        /// <returns>Commands with the specified tag.</returns>
        IReadOnlyList<ICommand> GetCommandsByTag(string tag);
        
        /// <summary>
        /// Gets commands by multiple tags (AND operation).
        /// </summary>
        /// <param name="tags">The tags to filter by.</param>
        /// <returns>Commands that have all the specified tags.</returns>
        IReadOnlyList<ICommand> GetCommandsByTags(IEnumerable<string> tags);
        
        /// <summary>
        /// Searches for commands by name or description.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <returns>Commands matching the search term.</returns>
        IReadOnlyList<ICommand> SearchCommands(string searchTerm);
        
        /// <summary>
        /// Gets commands that can be executed in parallel.
        /// </summary>
        /// <returns>Commands that support parallel execution.</returns>
        IReadOnlyList<ICommand> GetParallelExecutableCommands();
        
        /// <summary>
        /// Gets commands by priority level.
        /// </summary>
        /// <param name="priority">The priority level to filter by.</param>
        /// <returns>Commands with the specified priority.</returns>
        IReadOnlyList<ICommand> GetCommandsByPriority(CommandPriority priority);
        
        /// <summary>
        /// Gets commands that depend on a specific command.
        /// </summary>
        /// <param name="commandId">The ID of the command to find dependents for.</param>
        /// <returns>Commands that depend on the specified command.</returns>
        IReadOnlyList<ICommand> GetDependentCommands(string commandId);
        
        /// <summary>
        /// Gets commands that are dependencies of a specific command.
        /// </summary>
        /// <param name="commandId">The ID of the command to find dependencies for.</param>
        /// <returns>Commands that the specified command depends on.</returns>
        IReadOnlyList<ICommand> GetCommandDependencies(string commandId);
        
        /// <summary>
        /// Scans assemblies for commands and registers them automatically.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>Number of commands discovered and registered.</returns>
        int ScanAndRegisterCommands(IEnumerable<Assembly> assemblies);
        
        /// <summary>
        /// Scans a directory for plugin assemblies and registers their commands.
        /// </summary>
        /// <param name="pluginDirectory">The directory to scan for plugins.</param>
        /// <returns>Number of commands discovered and registered.</returns>
        int ScanAndRegisterPluginCommands(string pluginDirectory);
        
        /// <summary>
        /// Gets metadata about all registered commands.
        /// </summary>
        /// <returns>Command metadata for all registered commands.</returns>
        IReadOnlyList<CommandMetadata> GetAllCommandMetadata();
        
        /// <summary>
        /// Gets statistics about the command registry.
        /// </summary>
        /// <returns>Registry statistics.</returns>
        CommandRegistryStatistics GetStatistics();
        
        /// <summary>
        /// Clears all registered commands.
        /// </summary>
        void Clear();
        
        /// <summary>
        /// Event raised when a command is registered.
        /// </summary>
        event EventHandler<CommandRegisteredEventArgs> CommandRegistered;
        
        /// <summary>
        /// Event raised when a command is unregistered.
        /// </summary>
        event EventHandler<CommandUnregisteredEventArgs> CommandUnregistered;
    }
} 