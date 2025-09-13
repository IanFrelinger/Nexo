using System;
using System.Collections.Generic;
using System.Linq;
using Nexo.Core.Domain.Composition;
using Nexo.Core.Domain.Enums.Extensions;

namespace Nexo.Core.Domain.Models.Extensions
{
    /// <summary>
    /// Represents a request to generate a new extension/plugin using AI.
    /// </summary>
    public class ExtensionRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public ExtensionType Type { get; set; }
        public string TargetNamespace { get; set; } = string.Empty;
        public List<string> RequiredServices { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string Author { get; set; } = "AI Generated";
        public string Version { get; set; } = "1.0.0";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ExtensionPriority Priority { get; set; } = ExtensionPriority.Normal;
        public bool EnableCaching { get; set; } = true;
        public TimeSpan? Timeout { get; set; }

        /// <summary>
        /// Generates a detailed prompt for the AI to generate the extension code.
        /// </summary>
        public string ToPrompt()
        {
            var prompt = $@"
Generate a complete C# plugin implementation for the Nexo framework.

REQUIREMENTS:
- Plugin Name: {Name}
- Description: {Description}
- Type: {Type}
- Namespace: {TargetNamespace}
- Author: {Author}
- Version: {Version}

IMPLEMENTATION REQUIREMENTS:
1. Must implement the IPlugin interface from Nexo.Core.Domain.Interfaces
2. Must be a complete, compilable C# class
3. Must include proper error handling and logging
4. Must be thread-safe if applicable
5. Must include XML documentation for all public members

AVAILABLE SERVICES FOR DEPENDENCY INJECTION:
{string.Join("\n", RequiredServices.Select(s => $"- {s}"))}

PLUGIN INTERFACE DEFINITION:
```csharp
public interface IPlugin
{{
    string Name {{ get; }}
    string Version {{ get; }}
    string Description {{ get; }}
    string Author {{ get; }}
    bool IsEnabled {{ get; }}
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}}
```

ADDITIONAL PARAMETERS:
{string.Join("\n", Parameters.Select(p => $"- {p.Key}: {p.Value}"))}

Please generate a complete, production-ready C# plugin class that implements the above requirements.
The generated code should be ready to compile and run without any additional modifications.
";

            return prompt.Trim();
        }

        /// <summary>
        /// Validates the extension request for completeness and correctness.
        /// </summary>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (string.IsNullOrWhiteSpace(Name))
                result.AddError("Plugin name is required", "Name");

            if (string.IsNullOrWhiteSpace(Description))
                result.AddError("Plugin description is required", "Description");

            if (string.IsNullOrWhiteSpace(TargetNamespace))
                result.AddError("Target namespace is required", "TargetNamespace");

            if (RequiredServices.Count == 0)
                result.AddWarning("No required services specified - plugin will have limited functionality", "RequiredServices");

            return result;
        }
    }

}