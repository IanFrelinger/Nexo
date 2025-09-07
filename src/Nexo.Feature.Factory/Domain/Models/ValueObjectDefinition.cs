using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents the definition of a value object to be generated.
    /// </summary>
    public sealed class ValueObjectDefinition
    {
        private readonly List<PropertyDefinition> _properties = new();
        private readonly List<MethodDefinition> _methods = new();

        /// <summary>
        /// Gets the name of the value object.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the value object.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the namespace for the value object.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the list of properties for the value object.
        /// </summary>
        public IReadOnlyList<PropertyDefinition> Properties => _properties.AsReadOnly();

        /// <summary>
        /// Gets the list of methods for the value object.
        /// </summary>
        public IReadOnlyList<MethodDefinition> Methods => _methods.AsReadOnly();

        /// <summary>
        /// Gets whether the value object should include validation.
        /// </summary>
        public bool IncludeValidation { get; }

        /// <summary>
        /// Initializes a new instance of the ValueObjectDefinition class.
        /// </summary>
        /// <param name="name">The value object name</param>
        /// <param name="description">The value object description</param>
        /// <param name="namespace">The value object namespace</param>
        /// <param name="includeValidation">Whether to include validation</param>
        public ValueObjectDefinition(string name, string description, string @namespace, bool includeValidation = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            IncludeValidation = includeValidation;
        }

        /// <summary>
        /// Adds a property to the value object.
        /// </summary>
        /// <param name="property">The property definition</param>
        public void AddProperty(PropertyDefinition property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (_properties.Any(p => p.Name == property.Name))
                throw new InvalidOperationException($"Property '{property.Name}' already exists in value object '{Name}'");

            _properties.Add(property);
        }

        /// <summary>
        /// Adds a method to the value object.
        /// </summary>
        /// <param name="method">The method definition</param>
        public void AddMethod(MethodDefinition method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            _methods.Add(method);
        }
    }
}
