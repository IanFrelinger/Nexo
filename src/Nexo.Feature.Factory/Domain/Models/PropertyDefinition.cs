using System;
using System.Collections.Generic;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents the definition of a property for an entity or value object.
    /// </summary>
    public sealed class PropertyDefinition
    {
        private readonly List<ValidationRule> _validationRules = new();

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the property.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the description of the property.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets whether the property is required.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets whether the property is unique.
        /// </summary>
        public bool IsUnique { get; }

        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// Gets the list of validation rules for the property.
        /// </summary>
        public IReadOnlyList<ValidationRule> ValidationRules => _validationRules.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the PropertyDefinition class.
        /// </summary>
        /// <param name="name">The property name</param>
        /// <param name="type">The property type</param>
        /// <param name="description">The property description</param>
        /// <param name="isRequired">Whether the property is required</param>
        /// <param name="isUnique">Whether the property is unique</param>
        /// <param name="defaultValue">The default value</param>
        public PropertyDefinition(string name, string type, string description, bool isRequired = false, bool isUnique = false, object? defaultValue = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsRequired = isRequired;
            IsUnique = isUnique;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Adds a validation rule to the property.
        /// </summary>
        /// <param name="rule">The validation rule</param>
        public void AddValidationRule(ValidationRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            _validationRules.Add(rule);
        }
    }
}
