using System.Threading.Tasks;

namespace Nexo.CLI.Help
{
    /// <summary>
    /// Interface for interactive help system with searchable documentation and examples
    /// </summary>
    public interface IInteractiveHelpSystem
    {
        /// <summary>
        /// Shows interactive help for a specific topic or general help
        /// </summary>
        Task ShowInteractiveHelp(string? specificTopic = null);
        
        /// <summary>
        /// Shows help for a specific command
        /// </summary>
        Task ShowCommandHelp(string commandName);
        
        /// <summary>
        /// Searches documentation for a specific term
        /// </summary>
        Task SearchDocumentation(string searchTerm);
        
        /// <summary>
        /// Shows available examples
        /// </summary>
        Task ShowExamples(string? category = null);
        
        /// <summary>
        /// Shows the command browser
        /// </summary>
        Task ShowCommandBrowser();
    }
    
    /// <summary>
    /// Interface for documentation generation
    /// </summary>
    public interface IDocumentationGenerator
    {
        /// <summary>
        /// Generates comprehensive documentation for a command
        /// </summary>
        Task<string> GenerateCommandDocumentationAsync(string commandName);
        
        /// <summary>
        /// Searches documentation for a specific term
        /// </summary>
        Task<IEnumerable<DocumentationResult>> SearchDocumentationAsync(string searchTerm);
        
        /// <summary>
        /// Gets all available documentation topics
        /// </summary>
        Task<IEnumerable<DocumentationTopic>> GetAvailableTopicsAsync();
    }
    
    /// <summary>
    /// Interface for example repository
    /// </summary>
    public interface IExampleRepository
    {
        /// <summary>
        /// Gets examples by category
        /// </summary>
        Task<Dictionary<string, List<CommandExample>>> GetExamplesByCategory();
        
        /// <summary>
        /// Gets examples for a specific command
        /// </summary>
        Task<IEnumerable<CommandExample>> GetExamplesForCommandAsync(string commandName);
        
        /// <summary>
        /// Gets all available examples
        /// </summary>
        Task<IEnumerable<CommandExample>> GetAllExamplesAsync();
    }
    
    /// <summary>
    /// Represents a documentation search result
    /// </summary>
    public class DocumentationResult
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public double Relevance { get; set; }
        public string Content { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// Represents a documentation topic
    /// </summary>
    public class DocumentationTopic
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Keywords { get; set; } = new();
    }
    
    /// <summary>
    /// Represents a command example
    /// </summary>
    public class CommandExample
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CommandLine { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}
