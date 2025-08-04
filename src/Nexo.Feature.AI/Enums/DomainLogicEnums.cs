namespace Nexo.Feature.AI.Enums
{
    /// <summary>
    /// Types of domain entities.
    /// </summary>
    public enum EntityType
    {
        /// <summary>
        /// Core business entity.
        /// </summary>
        Core,
        
        /// <summary>
        /// Supporting entity.
        /// </summary>
        Supporting,
        
        /// <summary>
        /// Configuration entity.
        /// </summary>
        Configuration,
        
        /// <summary>
        /// Audit entity.
        /// </summary>
        Audit,
        
        /// <summary>
        /// Reference entity.
        /// </summary>
        Reference,
        
        /// <summary>
        /// Aggregate entity.
        /// </summary>
        Aggregate
    }

    /// <summary>
    /// Categories of business rules.
    /// </summary>
    public enum BusinessRuleCategory
    {
        /// <summary>
        /// Validation rule.
        /// </summary>
        Validation,
        
        /// <summary>
        /// Business logic rule.
        /// </summary>
        BusinessLogic,
        
        /// <summary>
        /// Authorization rule.
        /// </summary>
        Authorization,
        
        /// <summary>
        /// Workflow rule.
        /// </summary>
        Workflow,
        
        /// <summary>
        /// Compliance rule.
        /// </summary>
        Compliance
    }

    /// <summary>
    /// Status of requirements.
    /// </summary>
    public enum RequirementStatus
    {
        /// <summary>
        /// Draft status.
        /// </summary>
        Draft,
        
        /// <summary>
        /// Under review status.
        /// </summary>
        UnderReview,
        
        /// <summary>
        /// Approved status.
        /// </summary>
        Approved,
        
        /// <summary>
        /// Rejected status.
        /// </summary>
        Rejected,
        
        /// <summary>
        /// Implemented status.
        /// </summary>
        Implemented,
        
        /// <summary>
        /// Deprecated status.
        /// </summary>
        Deprecated
    }

    /// <summary>
    /// Types of entity methods.
    /// </summary>
    public enum MethodType
    {
        /// <summary>
        /// Constructor method.
        /// </summary>
        Constructor,
        
        /// <summary>
        /// Business logic method.
        /// </summary>
        BusinessLogic,
        
        /// <summary>
        /// Validation method.
        /// </summary>
        Validation,
        
        /// <summary>
        /// Factory method.
        /// </summary>
        Factory,
        
        /// <summary>
        /// Query method.
        /// </summary>
        Query,
        
        /// <summary>
        /// Command method.
        /// </summary>
        Command
    }

    /// <summary>
    /// Types of domain services.
    /// </summary>
    public enum ServiceType
    {
        /// <summary>
        /// Application service.
        /// </summary>
        Application,
        
        /// <summary>
        /// Domain service.
        /// </summary>
        Domain,
        
        /// <summary>
        /// Infrastructure service.
        /// </summary>
        Infrastructure,
        
        /// <summary>
        /// Integration service.
        /// </summary>
        Integration,
        
        /// <summary>
        /// Utility service.
        /// </summary>
        Utility
    }

    /// <summary>
    /// Types of domain events.
    /// </summary>
    public enum EventType
    {
        /// <summary>
        /// Entity created event.
        /// </summary>
        EntityCreated,
        
        /// <summary>
        /// Entity updated event.
        /// </summary>
        EntityUpdated,
        
        /// <summary>
        /// Entity deleted event.
        /// </summary>
        EntityDeleted,
        
        /// <summary>
        /// Business process event.
        /// </summary>
        BusinessProcess,
        
        /// <summary>
        /// System event.
        /// </summary>
        System,
        
        /// <summary>
        /// Integration event.
        /// </summary>
        Integration,
        
        /// <summary>
        /// Notification event.
        /// </summary>
        Notification
    }

    /// <summary>
    /// Types of validation rules.
    /// </summary>
    public enum ValidationType
    {
        /// <summary>
        /// Required field validation.
        /// </summary>
        Required,
        
        /// <summary>
        /// Length validation.
        /// </summary>
        Length,
        
        /// <summary>
        /// Range validation.
        /// </summary>
        Range,
        
        /// <summary>
        /// Pattern validation.
        /// </summary>
        Pattern,
        
        /// <summary>
        /// Custom validation.
        /// </summary>
        Custom,
        
        /// <summary>
        /// Business rule validation.
        /// </summary>
        BusinessRule,
        
        /// <summary>
        /// Cross-field validation.
        /// </summary>
        CrossField
    }

    /// <summary>
    /// Severity levels for validation.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Information level.
        /// </summary>
        Info,
        
        /// <summary>
        /// Warning level.
        /// </summary>
        Warning,
        
        /// <summary>
        /// Error level.
        /// </summary>
        Error,
        
        /// <summary>
        /// Critical level.
        /// </summary>
        Critical
    }

    /// <summary>
    /// Types of domain logic generation.
    /// </summary>
    public enum DomainLogicGenerationType
    {
        /// <summary>
        /// Full domain logic generation.
        /// </summary>
        Full,
        
        /// <summary>
        /// Entity-only generation.
        /// </summary>
        EntitiesOnly,
        
        /// <summary>
        /// Value objects only generation.
        /// </summary>
        ValueObjectsOnly,
        
        /// <summary>
        /// Services only generation.
        /// </summary>
        ServicesOnly,
        
        /// <summary>
        /// Events only generation.
        /// </summary>
        EventsOnly,
        
        /// <summary>
        /// Business rules only generation.
        /// </summary>
        BusinessRulesOnly
    }

    /// <summary>
    /// Types of domain logic optimization.
    /// </summary>
    public enum OptimizationType
    {
        /// <summary>
        /// Performance optimization.
        /// </summary>
        Performance,
        
        /// <summary>
        /// Maintainability optimization.
        /// </summary>
        Maintainability,
        
        /// <summary>
        /// Security optimization.
        /// </summary>
        Security,
        
        /// <summary>
        /// Scalability optimization.
        /// </summary>
        Scalability,
        
        /// <summary>
        /// Code quality optimization.
        /// </summary>
        CodeQuality,
        
        /// <summary>
        /// Architecture optimization.
        /// </summary>
        Architecture
    }

    /// <summary>
    /// Types of domain logic validation.
    /// </summary>
    public enum ValidationScope
    {
        /// <summary>
        /// Syntax validation.
        /// </summary>
        Syntax,
        
        /// <summary>
        /// Semantic validation.
        /// </summary>
        Semantic,
        
        /// <summary>
        /// Business rule validation.
        /// </summary>
        BusinessRules,
        
        /// <summary>
        /// Business validation.
        /// </summary>
        Business,
        
        /// <summary>
        /// Consistency validation.
        /// </summary>
        Consistency,
        
        /// <summary>
        /// Completeness validation.
        /// </summary>
        Completeness,
        
        /// <summary>
        /// Clarity validation.
        /// </summary>
        Clarity,
        
        /// <summary>
        /// Architecture validation.
        /// </summary>
        Architecture
    }
}