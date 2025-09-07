using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Base class for composable entities that provides common functionality for composition,
    /// validation, and metadata handling.
    /// </summary>
    /// <typeparam name="TId">The type of the entity identifier</typeparam>
    /// <typeparam name="TEntity">The type of the entity</typeparam>
    public abstract class ComposableEntity<TId, TEntity> : IComposable<TEntity>, IValidatable, IMetadataProvider
        where TId : IEquatable<TId>
        where TEntity : ComposableEntity<TId, TEntity>
    {
        protected readonly List<ValidationRule> _validationRules = new();
        protected readonly Dictionary<string, object> _metadata = new();
        
        /// <summary>
        /// Gets the unique identifier of this entity.
        /// </summary>
        public TId Id { get; protected set; }
        
        /// <summary>
        /// Gets the date and time when this entity was created.
        /// </summary>
        public DateTimeOffset CreatedAt { get; protected set; }
        
        /// <summary>
        /// Gets the date and time when this entity was last modified.
        /// </summary>
        public DateTimeOffset? ModifiedAt { get; protected set; }
        
        /// <summary>
        /// Gets all validation rules associated with this entity.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules => _validationRules.AsReadOnly();
        
        /// <summary>
        /// Gets all metadata associated with this entity.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();
        
        /// <summary>
        /// Initializes a new instance of the ComposableEntity class.
        /// </summary>
        /// <param name="id">The unique identifier for this entity</param>
        protected ComposableEntity(TId id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            CreatedAt = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Validates this entity using its validation rules.
        /// </summary>
        /// <returns>A validation result containing any validation errors or warnings</returns>
        public virtual ValidationResult Validate()
        {
            var result = new ValidationResult();
            
            // Validate the entity itself
            foreach (var rule in _validationRules)
            {
                if (!rule.Validate(this))
                {
                    if (rule.Severity == ValidationSeverity.Error || rule.Severity == ValidationSeverity.Critical)
                    {
                        result.AddError(rule.ErrorMessage, rule.Name, rule.Name);
                    }
                    else
                    {
                        result.AddWarning(rule.ErrorMessage, rule.Name, rule.Name);
                    }
                }
            }
            
            // Validate entity-specific rules
            var entityValidationResult = ValidateEntity();
            if (entityValidationResult != null)
            {
                result.Merge(entityValidationResult);
            }
            
            return result;
        }
        
        /// <summary>
        /// Gets all validation rules associated with this entity.
        /// </summary>
        /// <returns>An enumerable of validation rules</returns>
        public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
        
        /// <summary>
        /// Gets all metadata associated with this entity.
        /// </summary>
        /// <returns>A dictionary containing all metadata key-value pairs</returns>
        public virtual IDictionary<string, object> GetMetadata() => _metadata;
        
        /// <summary>
        /// Sets a metadata value for this entity.
        /// </summary>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        public virtual void SetMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
            
            _metadata[key] = value;
            UpdateModifiedTime();
        }
        
        /// <summary>
        /// Gets a specific metadata value.
        /// </summary>
        /// <typeparam name="TValue">The expected type of the metadata value</typeparam>
        /// <param name="key">The metadata key</param>
        /// <param name="defaultValue">The default value to return if the key doesn't exist</param>
        /// <returns>The metadata value if it exists and matches the expected type; otherwise, the default value</returns>
        public virtual TValue GetMetadata<TValue>(string key, TValue defaultValue = default!)
        {
            if (_metadata.TryGetValue(key, out var value) && value is TValue typedValue)
            {
                return typedValue;
            }
            
            return defaultValue;
        }
        
        /// <summary>
        /// Checks if a metadata key exists.
        /// </summary>
        /// <param name="key">The metadata key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        public virtual bool HasMetadata(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && _metadata.ContainsKey(key);
        }
        
        /// <summary>
        /// Removes a metadata key-value pair.
        /// </summary>
        /// <param name="key">The metadata key to remove</param>
        /// <returns>True if the key was removed, false if it didn't exist</returns>
        public virtual bool RemoveMetadata(string key)
        {
            var removed = !string.IsNullOrWhiteSpace(key) && _metadata.Remove(key);
            if (removed)
            {
                UpdateModifiedTime();
            }
            return removed;
        }
        
        /// <summary>
        /// Adds a validation rule to this entity.
        /// </summary>
        /// <param name="rule">The validation rule to add</param>
        protected virtual void AddValidationRule(ValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _validationRules.Add(rule);
        }
        
        /// <summary>
        /// Removes a validation rule from this entity.
        /// </summary>
        /// <param name="rule">The validation rule to remove</param>
        /// <returns>True if the rule was removed, false if it didn't exist</returns>
        protected virtual bool RemoveValidationRule(ValidationRule rule)
        {
            return rule != null && _validationRules.Remove(rule);
        }
        
        /// <summary>
        /// Clears all validation rules from this entity.
        /// </summary>
        protected virtual void ClearValidationRules()
        {
            _validationRules.Clear();
        }
        
        /// <summary>
        /// Updates the last modified timestamp of this entity.
        /// </summary>
        protected virtual void UpdateModifiedTime()
        {
            ModifiedAt = DateTimeOffset.UtcNow;
        }
        
        /// <summary>
        /// Composes this entity with other entities of the same type.
        /// This method must be implemented by derived classes to define how composition works.
        /// </summary>
        /// <param name="components">The components to compose with this entity</param>
        /// <returns>A new composed entity</returns>
        public abstract TEntity Compose(params TEntity[] components);
        
        /// <summary>
        /// Determines if this entity can be composed with another entity.
        /// This method must be implemented by derived classes to define composition compatibility.
        /// </summary>
        /// <param name="other">The other entity to check composition compatibility with</param>
        /// <returns>True if composition is possible, false otherwise</returns>
        public abstract bool CanComposeWith(TEntity other);
        
        /// <summary>
        /// Decomposes this entity into its constituent parts.
        /// This method must be implemented by derived classes to define how decomposition works.
        /// </summary>
        /// <returns>An enumerable of the decomposed components</returns>
        public abstract IEnumerable<TEntity> Decompose();
        
        /// <summary>
        /// Validates entity-specific business rules.
        /// This method can be overridden by derived classes to add entity-specific validation logic.
        /// </summary>
        /// <returns>A validation result for entity-specific validation, or null if no entity-specific validation is needed</returns>
        protected virtual ValidationResult ValidateEntity()
        {
            return null;
        }
        
        /// <summary>
        /// Determines if this entity equals another entity.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the entities are equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            if (obj is TEntity other)
            {
                return Equals(other);
            }
            return false;
        }
        
        /// <summary>
        /// Determines if this entity equals another entity.
        /// </summary>
        /// <param name="other">The other entity to compare with</param>
        /// <returns>True if the entities are equal, false otherwise</returns>
        public virtual bool Equals(TEntity other)
        {
            if (other == null) return false;
            return Id.Equals(other.Id);
        }
        
        /// <summary>
        /// Gets the hash code for this entity.
        /// </summary>
        /// <returns>The hash code</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        
        /// <summary>
        /// Returns a string representation of this entity.
        /// </summary>
        /// <returns>A string representation of the entity</returns>
        public override string ToString()
        {
            return $"{GetType().Name}[{Id}]";
        }
        
        /// <summary>
        /// Creates a composite entity from multiple components.
        /// </summary>
        /// <param name="components">The components to compose</param>
        /// <returns>A new composite entity</returns>
        public static TEntity CreateComposite(params TEntity[] components)
        {
            if (components == null || components.Length == 0)
                throw new ArgumentException("At least one component is required for composition", nameof(components));
            
            if (components.Length == 1)
                return components[0];
            
            return components[0].Compose(components.Skip(1).ToArray());
        }
        
        /// <summary>
        /// Validates multiple entities and returns a combined validation result.
        /// </summary>
        /// <param name="entities">The entities to validate</param>
        /// <returns>A combined validation result</returns>
        public static ValidationResult ValidateAll(params TEntity[] entities)
        {
            var results = entities?.Select(e => e?.Validate()) ?? Array.Empty<ValidationResult>();
            return ValidationResult.Combine(results.ToArray());
        }
        
        /// <summary>
        /// Merges metadata from multiple entities into a single metadata dictionary.
        /// </summary>
        /// <param name="entities">The entities to merge metadata from</param>
        /// <returns>A merged metadata dictionary</returns>
        public static IDictionary<string, object> MergeMetadata(params TEntity[] entities)
        {
            var mergedMetadata = new Dictionary<string, object>();
            
            foreach (var entity in entities ?? Array.Empty<TEntity>())
            {
                if (entity != null)
                {
                    foreach (var kvp in entity.GetMetadata())
                    {
                        mergedMetadata[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return mergedMetadata;
        }
        
        /// <summary>
        /// Equality operator for entities.
        /// </summary>
        /// <param name="left">The left entity</param>
        /// <param name="right">The right entity</param>
        /// <returns>True if the entities are equal, false otherwise</returns>
        public static bool operator ==(ComposableEntity<TId, TEntity> left, ComposableEntity<TId, TEntity> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }
        
        /// <summary>
        /// Inequality operator for entities.
        /// </summary>
        /// <param name="left">The left entity</param>
        /// <param name="right">The right entity</param>
        /// <returns>True if the entities are not equal, false otherwise</returns>
        public static bool operator !=(ComposableEntity<TId, TEntity> left, ComposableEntity<TId, TEntity> right)
        {
            return !(left == right);
        }
    }
} 