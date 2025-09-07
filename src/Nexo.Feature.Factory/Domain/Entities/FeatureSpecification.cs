using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Factory.Domain.Enums;
using Nexo.Feature.Factory.Domain.Models;
using Nexo.Feature.Factory.Domain.ValueObjects;

namespace Nexo.Feature.Factory.Domain.Entities
{
    /// <summary>
    /// Represents a feature specification that defines what needs to be generated.
    /// This is the core entity that drives the AI-native feature factory.
    /// </summary>
    public sealed class FeatureSpecification
    {
        private readonly List<EntityDefinition> _entities = new();
        private readonly List<ValueObjectDefinition> _valueObjects = new();
        private readonly List<BusinessRule> _businessRules = new();
        private readonly List<ValidationRule> _validationRules = new();
        private readonly Dictionary<string, object> _metadata = new();

        /// <summary>
        /// Gets the unique identifier for this feature specification.
        /// </summary>
        public FeatureSpecificationId Id { get; private set; }

        /// <summary>
        /// Gets the natural language description of the feature.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the target platform for code generation.
        /// </summary>
        public TargetPlatform TargetPlatform { get; private set; }

        /// <summary>
        /// Gets the execution strategy determined by the decision engine.
        /// </summary>
        public ExecutionStrategy ExecutionStrategy { get; private set; }

        /// <summary>
        /// Gets the list of entities to be generated.
        /// </summary>
        public IReadOnlyList<EntityDefinition> Entities => _entities.AsReadOnly();

        /// <summary>
        /// Gets the list of value objects to be generated.
        /// </summary>
        public IReadOnlyList<ValueObjectDefinition> ValueObjects => _valueObjects.AsReadOnly();

        /// <summary>
        /// Gets the list of business rules to be implemented.
        /// </summary>
        public IReadOnlyList<BusinessRule> BusinessRules => _businessRules.AsReadOnly();

        /// <summary>
        /// Gets the list of validation rules to be implemented.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules => _validationRules.AsReadOnly();

        /// <summary>
        /// Gets the metadata associated with this specification.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();

        /// <summary>
        /// Gets the date and time when this specification was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }

        /// <summary>
        /// Gets the current status of the specification.
        /// </summary>
        public FeatureSpecificationStatus Status { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FeatureSpecification class.
        /// </summary>
        /// <param name="id">The unique identifier</param>
        /// <param name="description">The natural language description</param>
        /// <param name="targetPlatform">The target platform</param>
        public FeatureSpecification(FeatureSpecificationId id, string description, TargetPlatform targetPlatform)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            TargetPlatform = targetPlatform;
            CreatedAt = DateTimeOffset.UtcNow;
            Status = FeatureSpecificationStatus.Draft;
            ExecutionStrategy = ExecutionStrategy.Generated; // Default strategy
        }

        /// <summary>
        /// Adds an entity definition to the specification.
        /// </summary>
        /// <param name="entity">The entity definition to add</param>
        public void AddEntity(EntityDefinition entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (_entities.Any(e => e.Name == entity.Name))
                throw new InvalidOperationException($"Entity '{entity.Name}' already exists in this specification");

            _entities.Add(entity);
        }

        /// <summary>
        /// Adds a value object definition to the specification.
        /// </summary>
        /// <param name="valueObject">The value object definition to add</param>
        public void AddValueObject(ValueObjectDefinition valueObject)
        {
            if (valueObject == null) throw new ArgumentNullException(nameof(valueObject));
            if (_valueObjects.Any(vo => vo.Name == valueObject.Name))
                throw new InvalidOperationException($"Value object '{valueObject.Name}' already exists in this specification");

            _valueObjects.Add(valueObject);
        }

        /// <summary>
        /// Adds a business rule to the specification.
        /// </summary>
        /// <param name="rule">The business rule to add</param>
        public void AddBusinessRule(BusinessRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _businessRules.Add(rule);
        }

        /// <summary>
        /// Adds a validation rule to the specification.
        /// </summary>
        /// <param name="rule">The validation rule to add</param>
        public void AddValidationRule(ValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _validationRules.Add(rule);
        }

        /// <summary>
        /// Sets the execution strategy for this specification.
        /// </summary>
        /// <param name="strategy">The execution strategy</param>
        public void SetExecutionStrategy(ExecutionStrategy strategy)
        {
            ExecutionStrategy = strategy;
        }

        /// <summary>
        /// Updates the status of the specification.
        /// </summary>
        /// <param name="status">The new status</param>
        public void UpdateStatus(FeatureSpecificationStatus status)
        {
            Status = status;
        }

        /// <summary>
        /// Adds metadata to the specification.
        /// </summary>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        public void AddMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Key cannot be null or empty", nameof(key));
            _metadata[key] = value;
        }

        /// <summary>
        /// Validates the feature specification.
        /// </summary>
        /// <returns>True if the specification is valid, false otherwise</returns>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Description) &&
                   _entities.Count > 0 &&
                   Status != FeatureSpecificationStatus.Invalid;
        }
    }
}
