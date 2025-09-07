using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Factory.Domain.Models
{
    /// <summary>
    /// Represents the definition of a method for an entity or value object.
    /// </summary>
    public sealed class MethodDefinition
    {
        private readonly List<ParameterDefinition> _parameters = new();

        /// <summary>
        /// Gets the name of the method.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the return type of the method.
        /// </summary>
        public string ReturnType { get; }

        /// <summary>
        /// Gets the description of the method.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the access modifier for the method.
        /// </summary>
        public AccessModifier AccessModifier { get; }

        /// <summary>
        /// Gets whether the method is async.
        /// </summary>
        public bool IsAsync { get; }

        /// <summary>
        /// Gets the list of parameters for the method.
        /// </summary>
        public IReadOnlyList<ParameterDefinition> Parameters => _parameters.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the MethodDefinition class.
        /// </summary>
        /// <param name="name">The method name</param>
        /// <param name="returnType">The return type</param>
        /// <param name="description">The method description</param>
        /// <param name="accessModifier">The access modifier</param>
        /// <param name="isAsync">Whether the method is async</param>
        public MethodDefinition(string name, string returnType, string description, AccessModifier accessModifier = AccessModifier.Public, bool isAsync = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AccessModifier = accessModifier;
            IsAsync = isAsync;
        }

        /// <summary>
        /// Adds a parameter to the method.
        /// </summary>
        /// <param name="parameter">The parameter definition</param>
        public void AddParameter(ParameterDefinition parameter)
        {
            if (parameter == null) throw new ArgumentNullException(nameof(parameter));
            if (_parameters.Any(p => p.Name == parameter.Name))
                throw new InvalidOperationException($"Parameter '{parameter.Name}' already exists in method '{Name}'");

            _parameters.Add(parameter);
        }
    }

    /// <summary>
    /// Represents the definition of a method parameter.
    /// </summary>
    public sealed class ParameterDefinition
    {
        /// <summary>
        /// Gets the name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the parameter.
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// Gets the description of the parameter.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets whether the parameter is optional.
        /// </summary>
        public bool IsOptional { get; }

        /// <summary>
        /// Gets the default value for the parameter.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// Initializes a new instance of the ParameterDefinition class.
        /// </summary>
        /// <param name="name">The parameter name</param>
        /// <param name="type">The parameter type</param>
        /// <param name="description">The parameter description</param>
        /// <param name="isOptional">Whether the parameter is optional</param>
        /// <param name="defaultValue">The default value</param>
        public ParameterDefinition(string name, string type, string description, bool isOptional = false, object? defaultValue = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            IsOptional = isOptional;
            DefaultValue = defaultValue;
        }
    }

    /// <summary>
    /// Represents the access modifier for methods and properties.
    /// </summary>
    public enum AccessModifier
    {
        Public,
        Private,
        Protected,
        Internal,
        ProtectedInternal
    }
}
