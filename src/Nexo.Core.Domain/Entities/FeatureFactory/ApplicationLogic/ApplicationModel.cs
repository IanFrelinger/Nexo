using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Nexo.Core.Domain.Entities.FeatureFactory.ApplicationLogic
{
    /// <summary>
    /// Represents an application model in the generated application logic
    /// </summary>
    public class ApplicationModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public List<ModelProperty> Properties { get; set; } = new();
        public List<ModelMethod> Methods { get; set; } = new();
        public List<ModelAttribute> Attributes { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public ModelType Type { get; set; } = ModelType.DTO;
        public string GeneratedCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a model property
    /// </summary>
    public class ModelProperty
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsNullable { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public List<ValidationAttribute> ValidationAttributes { get; set; } = new();
        public List<PropertyAttribute> Attributes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a model method
    /// </summary>
    public class ModelMethod
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
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
    /// Represents a model attribute
    /// </summary>
    public class ModelAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a property attribute
    /// </summary>
    public class PropertyAttribute
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Types of application models
    /// </summary>
    public enum ModelType
    {
        DTO,
        ViewModel,
        RequestModel,
        ResponseModel,
        EntityModel,
        DomainModel,
        ConfigurationModel,
        SettingsModel
    }
}
