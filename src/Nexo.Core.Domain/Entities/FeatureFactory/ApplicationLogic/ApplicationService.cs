using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic
{
    /// <summary>
    /// Represents an application service in the generated application logic
    /// </summary>
    public class ApplicationService
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string InterfaceName { get; set; } = string.Empty;
        public List<ServiceMethod> Methods { get; set; } = new();
        public List<ServiceProperty> Properties { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public List<string> Interfaces { get; set; } = new();
        public ServiceType Type { get; set; } = ServiceType.Application;
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a service method
    /// </summary>
    public class ServiceMethod
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public List<MethodParameter> Parameters { get; set; } = new();
        public string Implementation { get; set; } = string.Empty;
        public MethodVisibility Visibility { get; set; } = MethodVisibility.Public;
        public bool IsAsync { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsOverride { get; set; }
        public List<MethodAttribute> Attributes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a service property
    /// </summary>
    public class ServiceProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }
        public PropertyVisibility Visibility { get; set; } = PropertyVisibility.Public;
        public string DefaultValue { get; set; } = string.Empty;
        public List<ValidationAttribute> ValidationAttributes { get; set; } = new();
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
        public List<ValidationAttribute> ValidationAttributes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a method attribute
    /// </summary>
    public class MethodAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of application services
    /// </summary>
    public enum ServiceType
    {
        Application,
        Business,
        Data,
        Infrastructure,
        External,
        Integration
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

    /// <summary>
    /// Property visibility levels
    /// </summary>
    public enum PropertyVisibility
    {
        Public,
        Private,
        Protected,
        Internal
    }
}
