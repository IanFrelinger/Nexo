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
        Validation
    }

    /// <summary>
    /// Status of requirements.
    /// </summary>
    public enum RequirementStatus
    {
        Draft,
        InReview,
        Approved,
        Implemented,
        Rejected
    }

    /// <summary>
    /// Types of entity methods.
    /// </summary>
    public enum MethodType
    {
    }

    /// <summary>
    /// Types of domain services.
    /// </summary>
    public enum ServiceType
    {
        /// <summary>
        /// Domain service.
        /// </summary>
        Domain
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
        EntityUpdated
    }

    /// <summary>
    /// Types of validation rules.
    /// </summary>
    public enum ValidationType
    {
    }

    /// <summary>
    /// Severity levels for validation.
    /// </summary>
    public enum ValidationSeverity
    {
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
        Clarity
    }
}