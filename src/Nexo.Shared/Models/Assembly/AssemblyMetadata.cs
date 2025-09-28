using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nexo.Shared.Models.Assembly
{
    /// <summary>
    /// Represents metadata about a .NET assembly
    /// </summary>
    public class AssemblyMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public Version Version { get; set; } = new Version();
        public string Culture { get; set; } = string.Empty;
        public string PublicKeyToken { get; set; } = string.Empty;
        public AssemblyNameFlags Flags { get; set; }
        public string ProcessorArchitecture { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
        public long FileSize { get; set; }
        public string Hash { get; set; } = string.Empty;
        public IReadOnlyList<AssemblyReference> References { get; set; } = new List<AssemblyReference>();
        public IReadOnlyList<TypeMetadata> Types { get; set; } = new List<TypeMetadata>();
        public IReadOnlyList<ResourceMetadata> Resources { get; set; } = new List<ResourceMetadata>();
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
    }

    /// <summary>
    /// Represents a reference to another assembly
    /// </summary>
    public class AssemblyReference
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Version Version { get; set; } = new Version();
        public string Culture { get; set; } = string.Empty;
        public string PublicKeyToken { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public bool IsLoaded { get; set; }
        public string LoadError { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents metadata about a type in an assembly
    /// </summary>
    public class TypeMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public TypeAttributes Attributes { get; set; }
        public string BaseType { get; set; } = string.Empty;
        public IReadOnlyList<string> Interfaces { get; set; } = new List<string>();
        public IReadOnlyList<MethodMetadata> Methods { get; set; } = new List<MethodMetadata>();
        public IReadOnlyList<PropertyMetadata> Properties { get; set; } = new List<PropertyMetadata>();
        public IReadOnlyList<FieldMetadata> Fields { get; set; } = new List<FieldMetadata>();
        public IReadOnlyList<EventMetadata> Events { get; set; } = new List<EventMetadata>();
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
        public bool IsGeneric { get; set; }
        public IReadOnlyList<string> GenericParameters { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents metadata about a method
    /// </summary>
    public class MethodMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public MethodAttributes Attributes { get; set; }
        public string ReturnType { get; set; } = string.Empty;
        public IReadOnlyList<ParameterMetadata> Parameters { get; set; } = new List<ParameterMetadata>();
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
        public bool IsGeneric { get; set; }
        public IReadOnlyList<string> GenericParameters { get; set; } = new List<string>();
        public int ILSize { get; set; }
        public bool HasImplementation { get; set; }
    }

    /// <summary>
    /// Represents metadata about a property
    /// </summary>
    public class PropertyMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string PropertyType { get; set; } = string.Empty;
        public PropertyAttributes Attributes { get; set; }
        public MethodMetadata? GetMethod { get; set; }
        public MethodMetadata? SetMethod { get; set; }
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
    }

    /// <summary>
    /// Represents metadata about a field
    /// </summary>
    public class FieldMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FieldType { get; set; } = string.Empty;
        public FieldAttributes Attributes { get; set; }
        public object? DefaultValue { get; set; }
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
    }

    /// <summary>
    /// Represents metadata about an event
    /// </summary>
    public class EventMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public EventAttributes Attributes { get; set; }
        public MethodMetadata? AddMethod { get; set; }
        public MethodMetadata? RemoveMethod { get; set; }
        public MethodMetadata? RaiseMethod { get; set; }
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
    }

    /// <summary>
    /// Represents metadata about a method parameter
    /// </summary>
    public class ParameterMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string ParameterType { get; set; } = string.Empty;
        public ParameterAttributes Attributes { get; set; }
        public object? DefaultValue { get; set; }
        public int Position { get; set; }
        public IReadOnlyList<CustomAttributeMetadata> CustomAttributes { get; set; } = new List<CustomAttributeMetadata>();
    }

    /// <summary>
    /// Represents metadata about a custom attribute
    /// </summary>
    public class CustomAttributeMetadata
    {
        public string TypeName { get; set; } = string.Empty;
        public string FullTypeName { get; set; } = string.Empty;
        public IReadOnlyList<CustomAttributeArgument> ConstructorArguments { get; set; } = new List<CustomAttributeArgument>();
        public IReadOnlyList<CustomAttributeNamedArgument> NamedArguments { get; set; } = new List<CustomAttributeNamedArgument>();
    }

    /// <summary>
    /// Represents a custom attribute constructor argument
    /// </summary>
    public class CustomAttributeArgument
    {
        public string TypeName { get; set; } = string.Empty;
        public object? Value { get; set; }
    }

    /// <summary>
    /// Represents a custom attribute named argument
    /// </summary>
    public class CustomAttributeNamedArgument
    {
        public string Name { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public object? Value { get; set; }
        public bool IsField { get; set; }
    }

    /// <summary>
    /// Represents metadata about an embedded resource
    /// </summary>
    public class ResourceMetadata
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public ResourceLocation Location { get; set; }
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    /// <summary>
    /// Represents the location of a resource
    /// </summary>
    public enum ResourceLocation
    {
        Embedded,
        Contained,
        Manifest
    }
}
