using System;
using System.Collections.Generic;

namespace Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic
{
    /// <summary>
    /// Represents a business rule in the generated domain logic
    /// </summary>
    public class BusinessRule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string Implementation { get; set; } = string.Empty;
        public BusinessRuleType Type { get; set; } = BusinessRuleType.Validation;
        public BusinessRulePriority Priority { get; set; } = BusinessRulePriority.Medium;
        public List<string> Dependencies { get; set; } = new();
        public List<string> AffectedEntities { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a domain service in the generated domain logic
    /// </summary>
    public class DomainService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<ServiceMethod> Methods { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a method of a domain service
    /// </summary>
    public class ServiceMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public bool IsVirtual { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents an aggregate root in the generated domain logic
    /// </summary>
    public class AggregateRoot
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<EntityProperty> Properties { get; set; } = new();
        public List<EntityMethod> Methods { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public List<DomainEvent> DomainEvents { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a domain event
    /// </summary>
    public class DomainEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EventProperty> Properties { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a property of a domain event
    /// </summary>
    public class EventProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of business rules
    /// </summary>
    public enum BusinessRuleType
    {
        Validation,
        Constraint,
        Invariant,
        Policy,
        Calculation,
        Transformation
    }

    /// <summary>
    /// Business rule priority levels
    /// </summary>
    public enum BusinessRulePriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}
