namespace Nexo.Feature.Pipeline.Enums
{
    /// <summary>
    /// Categories for organizing commands in the pipeline architecture.
    /// </summary>
    public enum CommandCategory
    {
        /// <summary>
        /// File system operations (read, write, copy, delete, etc.).
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// Container operations (Docker, Podman, etc.).
        /// </summary>
        Container,
        
        /// <summary>
        /// Code analysis operations (static analysis, linting, etc.).
        /// </summary>
        Analysis,
        
        /// <summary>
        /// Project operations (initialization, scaffolding, etc.).
        /// </summary>
        Project,
        
        /// <summary>
        /// CLI operations (command execution, output formatting, etc.).
        /// </summary>
        CLI,
        
        /// <summary>
        /// Template operations (generation, processing, etc.).
        /// </summary>
        Template,
        
        /// <summary>
        /// Validation operations (input validation, schema validation, etc.).
        /// </summary>
        Validation,
        
        /// <summary>
        /// Agent operations (AI agents, automation, etc.).
        /// </summary>
        Agent,
        
        /// <summary>
        /// Plugin operations (loading, management, etc.).
        /// </summary>
        Plugin,
        
        /// <summary>
        /// Platform operations (OS-specific operations, etc.).
        /// </summary>
        Platform,
        
        /// <summary>
        /// Logging operations (log management, formatting, etc.).
        /// </summary>
        Logging,
        
        /// <summary>
        /// Configuration operations (settings management, etc.).
        /// </summary>
        Configuration,
        
        /// <summary>
        /// Network operations (HTTP requests, API calls, etc.).
        /// </summary>
        Network,
        
        /// <summary>
        /// Database operations (queries, migrations, etc.).
        /// </summary>
        Database,
        
        /// <summary>
        /// Security operations (encryption, authentication, etc.).
        /// </summary>
        Security,
        
        /// <summary>
        /// Testing operations (unit tests, integration tests, etc.).
        /// </summary>
        Testing,
        
        /// <summary>
        /// Build operations (compilation, packaging, etc.).
        /// </summary>
        Build,
        
        /// <summary>
        /// Deployment operations (deployment, rollback, etc.).
        /// </summary>
        Deployment,
        
        /// <summary>
        /// Monitoring operations (metrics, health checks, etc.).
        /// </summary>
        Monitoring,
        
        /// <summary>
        /// Utility operations (general utilities, helpers, etc.).
        /// </summary>
        Utility,
        
        /// <summary>
        /// Custom operations (user-defined commands).
        /// </summary>
        Custom
    }
} 