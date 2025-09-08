using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Core.Domain.Composition
{
    /// <summary>
    /// Base class for composable value objects that provides common functionality for composition,
    /// validation, and metadata handling.
    /// </summary>
    /// <typeparam name="T">The type of the value object</typeparam>
    public abstract class ComposableValueObject<T> : IComposable<T>, IValidatable, IMetadataProvider, IEquatable<T>
        where T : ComposableValueObject<T>
    {
        protected readonly List<ValidationRule> _validationRules = new();
        protected readonly Dictionary<string, object> _metadata = new();
        
        /// <summary>
        /// Gets all validation rules associated with this value object.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules => _validationRules.AsReadOnly();
        
        /// <summary>
        /// Gets all metadata associated with this value object.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata => _metadata.AsReadOnly();
        
        /// <summary>
        /// Validates this value object using its validation rules.
        /// </summary>
        /// <returns>A validation result containing any validation errors or warnings</returns>
        public virtual ValidationResult Validate()
        {
            var result = new ValidationResult();
            
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
            
            return result;
        }
        
        /// <summary>
        /// Gets all validation rules associated with this value object.
        /// </summary>
        /// <returns>An enumerable of validation rules</returns>
        public virtual IEnumerable<ValidationRule> GetValidationRules() => _validationRules;
        
        /// <summary>
        /// Gets all metadata associated with this value object.
        /// </summary>
        /// <returns>A dictionary containing all metadata key-value pairs</returns>
        public virtual IDictionary<string, object> GetMetadata() => _metadata;
        
        /// <summary>
        /// Sets a metadata value for this value object.
        /// </summary>
        /// <param name="key">The metadata key</param>
        /// <param name="value">The metadata value</param>
        public virtual void SetMetadata(string key, object value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Metadata key cannot be null or empty", nameof(key));
            
            _metadata[key] = value;
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
            return !string.IsNullOrWhiteSpace(key) && _metadata.Remove(key);
        }
        
        /// <summary>
        /// Adds a validation rule to this value object.
        /// </summary>
        /// <param name="rule">The validation rule to add</param>
        protected virtual void AddValidationRule(ValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _validationRules.Add(rule);
        }
        
        /// <summary>
        /// Removes a validation rule from this value object.
        /// </summary>
        /// <param name="rule">The validation rule to remove</param>
        /// <returns>True if the rule was removed, false if it didn't exist</returns>
        protected virtual bool RemoveValidationRule(ValidationRule rule)
        {
            return rule != null && _validationRules.Remove(rule);
        }
        
        /// <summary>
        /// Clears all validation rules from this value object.
        /// </summary>
        protected virtual void ClearValidationRules()
        {
            _validationRules.Clear();
        }
        
        /// <summary>
        /// Composes this value object with other value objects of the same type.
        /// This method must be implemented by derived classes to define how composition works.
        /// </summary>
        /// <param name="components">The components to compose with this value object</param>
        /// <returns>A new composed value object</returns>
        public abstract T Compose(params T[] components);
        
        /// <summary>
        /// Determines if this value object can be composed with another value object.
        /// This method must be implemented by derived classes to define composition compatibility.
        /// </summary>
        /// <param name="other">The other value object to check composition compatibility with</param>
        /// <returns>True if composition is possible, false otherwise</returns>
        public abstract bool CanComposeWith(T other);
        
        /// <summary>
        /// Decomposes this value object into its constituent parts.
        /// This method must be implemented by derived classes to define how decomposition works.
        /// </summary>
        /// <returns>An enumerable of the decomposed components</returns>
        public abstract IEnumerable<T> Decompose();
        
        /// <summary>
        /// Determines if this value object equals another value object.
        /// This method must be implemented by derived classes to define equality semantics.
        /// </summary>
        /// <param name="other">The other value object to compare with</param>
        /// <returns>True if the value objects are equal, false otherwise</returns>
        public abstract bool Equals(T? other);
        
        /// <summary>
        /// Determines if this value object equals another object.
        /// </summary>
        /// <param name="obj">The object to compare with</param>
        /// <returns>True if the objects are equal, false otherwise</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as T);
        }
        
        /// <summary>
        /// Gets the hash code for this value object.
        /// This method must be implemented by derived classes to define hash code semantics.
        /// </summary>
        /// <returns>The hash code</returns>
        public abstract override int GetHashCode();
        
        /// <summary>
        /// Returns a string representation of this value object.
        /// This method must be implemented by derived classes to define string representation.
        /// </summary>
        /// <returns>A string representation of the value object</returns>
        public abstract override string ToString();
        
        /// <summary>
        /// Creates a composite value object from multiple components.
        /// </summary>
        /// <param name="components">The components to compose</param>
        /// <returns>A new composite value object</returns>
        public static T CreateComposite(params T[] components)
        {
            if (components == null || components.Length == 0)
                throw new ArgumentException("At least one component is required for composition", nameof(components));
            
            if (components.Length == 1)
                return components[0];
            
            return components[0].Compose(components.Skip(1).ToArray());
        }
        
        /// <summary>
        /// Validates multiple value objects and returns a combined validation result.
        /// </summary>
        /// <param name="valueObjects">The value objects to validate</param>
        /// <returns>A combined validation result</returns>
        public static ValidationResult ValidateAll(params T[] valueObjects)
        {
            var results = valueObjects?.Select(vo => vo?.Validate()).Where(r => r != null).Cast<ValidationResult>() ?? Array.Empty<ValidationResult>();
            return ValidationResult.Combine(results.ToArray());
        }
        
        /// <summary>
        /// Merges metadata from multiple value objects into a single metadata dictionary.
        /// </summary>
        /// <param name="valueObjects">The value objects to merge metadata from</param>
        /// <returns>A merged metadata dictionary</returns>
        public static IDictionary<string, object> MergeMetadata(params T[] valueObjects)
        {
            var mergedMetadata = new Dictionary<string, object>();
            
            foreach (var valueObject in valueObjects ?? Array.Empty<T>())
            {
                if (valueObject != null)
                {
                    foreach (var kvp in valueObject.GetMetadata())
                    {
                        mergedMetadata[kvp.Key] = kvp.Value;
                    }
                }
            }
            
            return mergedMetadata;
        }
    }
} 