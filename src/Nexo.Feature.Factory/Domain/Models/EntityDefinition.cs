using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents the definition of an entity to be generated.
    /// </summary>
    public sealed class EntityDefinition
    {
        private readonly List<PropertyDefinition> _properties = new();
        private readonly List<MethodDefinition> _methods = new();
        private readonly List<string> _interfaces = new();

        /// <summary>
        /// Gets the name of the entity.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the entity.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the namespace for the entity.
        /// </summary>
        public string Namespace { get; }

        /// <summary>
        /// Gets the list of properties for the entity.
        /// </summary>
        public IReadOnlyList<PropertyDefinition> Properties => _properties.AsReadOnly();

        /// <summary>
        /// Gets the list of methods for the entity.
        /// </summary>
        public IReadOnlyList<MethodDefinition> Methods => _methods.AsReadOnly();

        /// <summary>
        /// Gets the list of interfaces the entity implements.
        /// </summary>
        public IReadOnlyList<string> Interfaces => _interfaces.AsReadOnly();

        /// <summary>
        /// Gets whether the entity should include CRUD operations.
        /// </summary>
        public bool IncludeCrudOperations { get; }

        /// <summary>
        /// Gets whether the entity should include validation.
        /// </summary>
        public bool IncludeValidation { get; }

        /// <summary>
        /// Initializes a new instance of the EntityDefinition class.
        /// </summary>
        /// <param name="name">The entity name</param>
        /// <param name="description">The entity description</param>
        /// <param name="namespace">The entity namespace</param>
        /// <param name="includeCrudOperations">Whether to include CRUD operations</param>
        /// <param name="includeValidation">Whether to include validation</param>
        public EntityDefinition(string name, string description, string @namespace, bool includeCrudOperations = true, bool includeValidation = true)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            Namespace = @namespace ?? throw new ArgumentNullException(nameof(@namespace));
            IncludeCrudOperations = includeCrudOperations;
            IncludeValidation = includeValidation;
        }

        /// <summary>
        /// Adds a property to the entity.
        /// </summary>
        /// <param name="property">The property definition</param>
        public void AddProperty(PropertyDefinition property)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (_properties.Any(p => p.Name == property.Name))
                throw new InvalidOperationException($"Property '{property.Name}' already exists in entity '{Name}'");

            _properties.Add(property);
        }

        /// <summary>
        /// Adds a method to the entity.
        /// </summary>
        /// <param name="method">The method definition</param>
        public void AddMethod(MethodDefinition method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            _methods.Add(method);
        }

        /// <summary>
        /// Adds an interface that the entity implements.
        /// </summary>
        /// <param name="interfaceName">The interface name</param>
        public void AddInterface(string interfaceName)
        {
            if (string.IsNullOrWhiteSpace(interfaceName)) throw new ArgumentException("Interface name cannot be null or empty", nameof(interfaceName));
            if (_interfaces.Contains(interfaceName))
                throw new InvalidOperationException($"Interface '{interfaceName}' already implemented by entity '{Name}'");

            _interfaces.Add(interfaceName);
        }
    }
}
