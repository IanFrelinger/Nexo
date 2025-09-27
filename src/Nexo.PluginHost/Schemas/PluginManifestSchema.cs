using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Nexo.PluginHost.Schemas
{
    /// <summary>
    /// Enhanced plugin manifest schema with validation attributes.
    /// </summary>
    public partial class PluginManifestSchema
    {
        /// <summary>
        /// Gets or sets the plugin name. Must be unique and follow naming conventions.
        /// </summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin version. Must follow semantic versioning.
        /// </summary>
        [Required]
        [RegularExpression(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$", 
            ErrorMessage = "Version must follow semantic versioning (e.g., 1.0.0, 1.0.0-alpha, 1.0.0+build)")]
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin description.
        /// </summary>
        [StringLength(500)]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin author.
        /// </summary>
        [StringLength(100)]
        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum required Nexo version.
        /// </summary>
        [Required]
        [RegularExpression(@"^\d+\.\d+\.\d+(-[a-zA-Z0-9\-\.]+)?(\+[a-zA-Z0-9\-\.]+)?$", 
            ErrorMessage = "MinimalNexoVersion must follow semantic versioning")]
        [JsonPropertyName("minimalNexoVersion")]
        public string MinimalNexoVersion { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the list of capabilities this plugin implements.
        /// </summary>
        [Required]
        [MinLength(1, ErrorMessage = "At least one capability must be declared")]
        [JsonPropertyName("capabilities")]
        public List<string> Capabilities { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the plugin dependencies.
        /// </summary>
        [JsonPropertyName("dependencies")]
        public List<PluginDependency> Dependencies { get; set; } = new List<PluginDependency>();

        /// <summary>
        /// Gets or sets the plugin configuration schema.
        /// </summary>
        [JsonPropertyName("configuration")]
        public PluginConfigurationSchema Configuration { get; set; } = new PluginConfigurationSchema();

        /// <summary>
        /// Gets or sets the plugin security requirements.
        /// </summary>
        [JsonPropertyName("security")]
        public PluginSecurityRequirements Security { get; set; } = new PluginSecurityRequirements();

        /// <summary>
        /// Gets or sets the plugin metadata.
        /// </summary>
        [JsonPropertyName("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the plugin creation timestamp.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the plugin last modified timestamp.
        /// </summary>
        [JsonPropertyName("lastModified")]
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the plugin license information.
        /// </summary>
        [JsonPropertyName("license")]
        public string License { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin homepage URL.
        /// </summary>
        [Url]
        [JsonPropertyName("homepage")]
        public string Homepage { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin repository URL.
        /// </summary>
        [Url]
        [JsonPropertyName("repository")]
        public string Repository { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the plugin documentation URL.
        /// </summary>
        [Url]
        [JsonPropertyName("documentation")]
        public string Documentation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a plugin dependency.
    /// </summary>
    public partial class PluginDependency
    {
        /// <summary>
        /// Gets or sets the dependency name.
        /// </summary>
        [Required]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dependency version.
        /// </summary>
        [Required]
        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the dependency is required.
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; } = true;

        /// <summary>
        /// Gets or sets the dependency type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "nuget";
    }

    /// <summary>
    /// Represents plugin configuration schema.
    /// </summary>
    public partial class PluginConfigurationSchema
    {
        /// <summary>
        /// Gets or sets the configuration properties.
        /// </summary>
        [JsonPropertyName("properties")]
        public Dictionary<string, ConfigurationProperty> Properties { get; set; } = new Dictionary<string, ConfigurationProperty>();

        /// <summary>
        /// Gets or sets whether additional properties are allowed.
        /// </summary>
        [JsonPropertyName("additionalProperties")]
        public bool AdditionalProperties { get; set; } = false;
    }

    /// <summary>
    /// Represents a configuration property.
    /// </summary>
    public partial class ConfigurationProperty
    {
        /// <summary>
        /// Gets or sets the property type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = "string";

        /// <summary>
        /// Gets or sets the property description.
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        [JsonPropertyName("default")]
        public object DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether the property is required.
        /// </summary>
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        /// <summary>
        /// Gets or sets the property validation rules.
        /// </summary>
        [JsonPropertyName("validation")]
        public Dictionary<string, object> Validation { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// Represents plugin security requirements.
    /// </summary>
    public partial class PluginSecurityRequirements
    {
        /// <summary>
        /// Gets or sets the required permissions.
        /// </summary>
        [JsonPropertyName("permissions")]
        public List<string> Permissions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed file system paths.
        /// </summary>
        [JsonPropertyName("allowedPaths")]
        public List<string> AllowedPaths { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed network endpoints.
        /// </summary>
        [JsonPropertyName("allowedEndpoints")]
        public List<string> AllowedEndpoints { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets whether the plugin requires elevated privileges.
        /// </summary>
        [JsonPropertyName("requiresElevation")]
        public bool RequiresElevation { get; set; } = false;

        /// <summary>
        /// Gets or sets the plugin sandbox level.
        /// </summary>
        [JsonPropertyName("sandboxLevel")]
        public string SandboxLevel { get; set; } = "standard";
    }
}
