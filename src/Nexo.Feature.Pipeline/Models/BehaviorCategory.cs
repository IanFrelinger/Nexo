namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Categories for organizing behaviors in the pipeline architecture.
    /// </summary>
    public enum BehaviorCategory
    {
        /// <summary>
        /// File system operations (file operations, directory management, etc.).
        /// </summary>
        FileSystem,
        
        /// <summary>
        /// Container operations (Docker, Podman, container management, etc.).
        /// </summary>
        Container,
        
        /// <summary>
        /// Code analysis operations (static analysis, linting, code review, etc.).
        /// </summary>
        Analysis,
        
        /// <summary>
        /// Project operations (initialization, scaffolding, project management, etc.).
        /// </summary>
        Project,
        
        /// <summary>
        /// CLI operations (command execution, output formatting, user interaction, etc.).
        /// </summary>
        CLI,
        
        /// <summary>
        /// Template operations (generation, processing, template management, etc.).
        /// </summary>
        Template,
        
        /// <summary>
        /// Validation operations (input validation, schema validation, quality checks, etc.).
        /// </summary>
        Validation,
        
        /// <summary>
        /// Agent operations (AI agents, automation, intelligent processing, etc.).
        /// </summary>
        Agent,
        
        /// <summary>
        /// Plugin operations (loading, management, plugin lifecycle, etc.).
        /// </summary>
        Plugin,
        
        /// <summary>
        /// Platform operations (OS-specific operations, platform abstraction, etc.).
        /// </summary>
        Platform,
        
        /// <summary>
        /// Logging operations (log management, formatting, log analysis, etc.).
        /// </summary>
        Logging,
        
        /// <summary>
        /// Configuration operations (settings management, configuration validation, etc.).
        /// </summary>
        Configuration,
        
        /// <summary>
        /// Network operations (HTTP requests, API calls, network management, etc.).
        /// </summary>
        Network,
        
        /// <summary>
        /// Database operations (queries, migrations, database management, etc.).
        /// </summary>
        Database,
        
        /// <summary>
        /// Security operations (encryption, authentication, security validation, etc.).
        /// </summary>
        Security,
        
        /// <summary>
        /// Testing operations (unit tests, integration tests, test automation, etc.).
        /// </summary>
        Testing,
        
        /// <summary>
        /// Build operations (compilation, packaging, build automation, etc.).
        /// </summary>
        Build,
        
        /// <summary>
        /// Deployment operations (deployment, rollback, deployment automation, etc.).
        /// </summary>
        Deployment,
        
        /// <summary>
        /// Monitoring operations (metrics, health checks, monitoring automation, etc.).
        /// </summary>
        Monitoring,
        
        /// <summary>
        /// Utility operations (general utilities, helpers, common operations, etc.).
        /// </summary>
        Utility,
        
        /// <summary>
        /// Custom operations (user-defined behaviors).
        /// </summary>
        Custom
    }
} 