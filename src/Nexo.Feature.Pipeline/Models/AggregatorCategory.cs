namespace Nexo.Feature.Pipeline.Models
{
    /// <summary>
    /// Categories for organizing aggregators in the pipeline architecture.
    /// </summary>
    public enum AggregatorCategory
    {
        /// <summary>
        /// Development workflow aggregators (complete development workflows).
        /// </summary>
        Development,
        
        /// <summary>
        /// Build workflow aggregators (build and compilation workflows).
        /// </summary>
        Build,
        
        /// <summary>
        /// Test workflow aggregators (testing and quality assurance workflows).
        /// </summary>
        Test,
        
        /// <summary>
        /// Deployment workflow aggregators (deployment and release workflows).
        /// </summary>
        Deployment,
        
        /// <summary>
        /// Analysis workflow aggregators (code analysis and review workflows).
        /// </summary>
        Analysis,
        
        /// <summary>
        /// Maintenance workflow aggregators (maintenance and cleanup workflows).
        /// </summary>
        Maintenance,
        
        /// <summary>
        /// Monitoring workflow aggregators (monitoring and observability workflows).
        /// </summary>
        Monitoring,
        
        /// <summary>
        /// Security workflow aggregators (security and compliance workflows).
        /// </summary>
        Security,
        
        /// <summary>
        /// Migration workflow aggregators (migration and upgrade workflows).
        /// </summary>
        Migration,
        
        /// <summary>
        /// Integration workflow aggregators (integration and API workflows).
        /// </summary>
        Integration,
        
        /// <summary>
        /// Documentation workflow aggregators (documentation generation workflows).
        /// </summary>
        Documentation,
        
        /// <summary>
        /// Backup workflow aggregators (backup and recovery workflows).
        /// </summary>
        Backup,
        
        /// <summary>
        /// Optimization workflow aggregators (performance optimization workflows).
        /// </summary>
        Optimization,
        
        /// <summary>
        /// Validation workflow aggregators (comprehensive validation workflows).
        /// </summary>
        Validation,
        
        /// <summary>
        /// Setup workflow aggregators (environment setup workflows).
        /// </summary>
        Setup,
        
        /// <summary>
        /// Cleanup workflow aggregators (cleanup and teardown workflows).
        /// </summary>
        Cleanup,
        
        /// <summary>
        /// Utility workflow aggregators (general utility workflows).
        /// </summary>
        Utility,
        
        /// <summary>
        /// Custom workflow aggregators (user-defined workflows).
        /// </summary>
        Custom
    }
} 