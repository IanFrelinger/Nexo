using Nexo.Core.Domain.Entities.FeatureFactory;
using Nexo.Core.Domain.Entities.Domain;
using Nexo.Core.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.FeatureFactory.DomainLogic
{
    /// <summary>
    /// Interface for generating domain logic from validated requirements
    /// </summary>
    public interface IDomainLogicGenerator
    {
        /// <summary>
        /// Generates complete domain logic from validated requirements
        /// </summary>
        Task<DomainLogicResult> GenerateDomainLogicAsync(ValidatedRequirements requirements, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates business entities from extracted requirements
        /// </summary>
        Task<BusinessEntityResult> GenerateBusinessEntitiesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates value objects from extracted requirements
        /// </summary>
        Task<ValueObjectResult> GenerateValueObjectsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates business rules from extracted requirements
        /// </summary>
        Task<BusinessRuleResult> GenerateBusinessRulesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates domain services from extracted requirements
        /// </summary>
        Task<DomainServiceResult> GenerateDomainServicesAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates aggregate roots from extracted requirements
        /// </summary>
        Task<AggregateRootResult> GenerateAggregateRootsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates domain events from extracted requirements
        /// </summary>
        Task<DomainEventResult> GenerateDomainEventsAsync(ExtractedRequirement requirement, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates repositories for domain entities
        /// </summary>
        Task<RepositoryResult> GenerateRepositoriesAsync(List<DomainEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates factories for domain entities
        /// </summary>
        Task<FactoryResult> GenerateFactoriesAsync(List<DomainEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates specifications for domain entities
        /// </summary>
        Task<SpecificationResult> GenerateSpecificationsAsync(List<DomainEntity> entities, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of domain logic generation
    /// </summary>
    public class DomainLogicResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<DomainEntity> Entities { get; set; } = new();
        public List<ValueObject> ValueObjects { get; set; } = new();
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public List<DomainService> DomainServices { get; set; } = new();
        public List<AggregateRoot> AggregateRoots { get; set; } = new();
        public List<DomainEvent> DomainEvents { get; set; } = new();
        public List<Repository> Repositories { get; set; } = new();
        public List<Factory> Factories { get; set; } = new();
        public List<Specification> Specifications { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of business entity generation
    /// </summary>
    public class BusinessEntityResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<DomainEntity> Entities { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of value object generation
    /// </summary>
    public class ValueObjectResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<ValueObject> ValueObjects { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of business rule generation
    /// </summary>
    public class BusinessRuleResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<BusinessRule> BusinessRules { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of domain service generation
    /// </summary>
    public class DomainServiceResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<DomainService> DomainServices { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of aggregate root generation
    /// </summary>
    public class AggregateRootResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<AggregateRoot> AggregateRoots { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of domain event generation
    /// </summary>
    public class DomainEventResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<DomainEvent> DomainEvents { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of repository generation
    /// </summary>
    public class RepositoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<Repository> Repositories { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of factory generation
    /// </summary>
    public class FactoryResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<Factory> Factories { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Result of specification generation
    /// </summary>
    public class SpecificationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<Specification> Specifications { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Repository for domain entities
    /// </summary>
    public class Repository
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public List<RepositoryMethod> Methods { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Method of a repository
    /// </summary>
    public class RepositoryMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Factory for domain entities
    /// </summary>
    public class Factory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public List<FactoryMethod> Methods { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Method of a factory
    /// </summary>
    public class FactoryMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Specification for domain entities
    /// </summary>
    public class Specification
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public List<SpecificationMethod> Methods { get; set; } = new();
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Method of a specification
    /// </summary>
    public class SpecificationMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
