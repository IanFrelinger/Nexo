using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.DomainLogic
{
    /// <summary>
    /// Represents a domain entity in the generated domain logic
    /// </summary>
    public class DomainEntity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<EntityProperty> Properties { get; set; } = new();
        public List<EntityMethod> Methods { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public EntityType Type { get; set; } = EntityType.AggregateRoot;
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a property of a domain entity
    /// </summary>
    public class EntityProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public List<ValidationAttribute> ValidationAttributes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a method of a domain entity
    /// </summary>
    public class EntityMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a method parameter
    /// </summary>
    public class MethodParameter
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of domain entities
    /// </summary>
    public enum EntityType
    {
        AggregateRoot,
        Entity,
        ValueObject,
        DomainService,
        Repository,
        Factory,
        Specification
    }

    /// <summary>
    /// Method visibility levels
    /// </summary>
    public enum MethodVisibility
    {
        Public,
        Private,
        Protected,
        Internal
    }
}
