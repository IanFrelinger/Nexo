namespace FeatureFactoryDemo.Commands
{
    /// <summary>
    /// Interface for all Feature Factory commands
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The command name
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Command description
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Command usage information
        /// </summary>
        string Usage { get; }
        
        /// <summary>
        /// Execute the command
        /// </summary>
        /// <param name="args">Command arguments</param>
        /// <returns>Exit code (0 for success, non-zero for failure)</returns>
        Task<int> ExecuteAsync(string[] args);
    }
}
