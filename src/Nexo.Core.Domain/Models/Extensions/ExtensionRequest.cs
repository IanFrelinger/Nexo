using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Common;
using Nexo.Core.Domain.Composition;
using Nexo.Core.Domain.Enums.Extensions;

namespace Nexo.Core.Domain.Models.Extensions
{
    /// <summary>
    /// Represents a request to generate a new extension/plugin
    /// </summary>
    public class ExtensionRequest : BaseRequest
    {
        /// <summary>
        /// Name of the extension
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of what the extension does
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Type of extension being generated
        /// </summary>
        public ExtensionType Type { get; set; } = ExtensionType.Custom;

        /// <summary>
        /// Author of the extension
        /// </summary>
        public string Author { get; set; } = string.Empty;

        /// <summary>
        /// Version of the extension
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Dependencies required by the extension
        /// </summary>
        public string[] Dependencies { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Tags associated with the extension
        /// </summary>
        public string[] Tags { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Additional configuration parameters
        /// </summary>
        public Dictionary<string, object> Configuration { get; set; } = new();

        /// <summary>
        /// Validates the extension request
        /// </summary>
        /// <returns>Validation result</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            // Validate Name
            if (string.IsNullOrEmpty(Name))
            {
                result.AddError("Name is required", nameof(Name));
            }
            else if (string.IsNullOrWhiteSpace(Name))
            {
                result.AddError("Name cannot be empty or whitespace", nameof(Name));
            }
            else if (Name.Trim() != Name)
            {
                result.AddError("Name cannot have leading or trailing whitespace", nameof(Name));
            }
            else if (Name.Length > 100)
            {
                result.AddError("Name cannot exceed 100 characters", nameof(Name));
            }

            // Validate Description
            if (string.IsNullOrEmpty(Description))
            {
                result.AddError("Description is required", nameof(Description));
            }
            else if (string.IsNullOrWhiteSpace(Description))
            {
                result.AddError("Description cannot be empty or whitespace", nameof(Description));
            }
            else if (Description.Trim() != Description)
            {
                result.AddError("Description cannot have leading or trailing whitespace", nameof(Description));
            }
            else if (Description.Length > 500)
            {
                result.AddError("Description cannot exceed 500 characters", nameof(Description));
            }

            // Validate Author (optional but if provided, should be valid)
            if (!string.IsNullOrWhiteSpace(Author) && Author.Length > 100)
            {
                result.AddError("Author cannot exceed 100 characters", nameof(Author));
            }

            // Validate Version (optional but if provided, should be valid)
            if (!string.IsNullOrWhiteSpace(Version) && !IsValidVersion(Version))
            {
                result.AddError("Version must be in valid format (e.g., 1.0.0)", nameof(Version));
            }

            // Validate Dependencies
            if (Dependencies != null)
            {
                foreach (var dependency in Dependencies)
                {
                    if (string.IsNullOrWhiteSpace(dependency))
                    {
                        result.AddError("Dependency cannot be empty", nameof(Dependencies));
                    }
                    else if (dependency.Length > 200)
                    {
                        result.AddError("Dependency name cannot exceed 200 characters", nameof(Dependencies));
                    }
                }
            }

            // Validate Tags
            if (Tags != null)
            {
                foreach (var tag in Tags)
                {
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        result.AddError("Tag cannot be empty", nameof(Tags));
                    }
                    else if (tag.Length > 50)
                    {
                        result.AddError("Tag cannot exceed 50 characters", nameof(Tags));
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Generates a prompt for AI code generation
        /// </summary>
        /// <returns>Formatted prompt string</returns>
        public string ToPrompt()
        {
            var prompt = $@"Generate a C# plugin that implements IPlugin interface with the following specifications:

Extension Details:
- Name: {Name}
- Description: {Description}
- Type: {Type}
- Author: {(string.IsNullOrWhiteSpace(Author) ? "Nexo AI" : Author)}
- Version: {Version}

Requirements:
- Must implement IPlugin interface from Nexo.Core.Application.Interfaces
- Must be a public class
- Must include proper error handling
- Must be thread-safe
- Must include XML documentation

Dependencies: {(Dependencies?.Length > 0 ? string.Join(", ", Dependencies) : "None")}

Tags: {(Tags?.Length > 0 ? string.Join(", ", Tags) : "None")}

Additional Configuration:
{string.Join(Environment.NewLine, Configuration.Select(kvp => $"- {kvp.Key}: {kvp.Value}"))}

Please generate a complete, compilable C# class that follows these specifications. Include:
1. Using statements
2. Namespace declaration
3. Class declaration implementing IPlugin
4. All required interface members
5. Implementation logic based on the description
6. XML documentation comments
7. Error handling
8. Thread safety considerations

The generated code should be production-ready and follow C# best practices.";

            return prompt;
        }

        /// <summary>
        /// Validates version string format
        /// </summary>
        /// <param name="version">Version string to validate</param>
        /// <returns>True if valid version format</returns>
        private static bool IsValidVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return false;

            var parts = version.Split('.');
            if (parts.Length < 2 || parts.Length > 4)
                return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out var number) || number < 0)
                    return false;
            }

            return true;
        }
    }
}
