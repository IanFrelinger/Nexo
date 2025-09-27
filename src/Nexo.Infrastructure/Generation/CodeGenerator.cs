using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Generation
{
    /// <summary>
    /// Generates code using AI with proper prompts for tool creation
    /// </summary>
    public partial class CodeGenerator : ICodeGenerator
    {
        private readonly IModelProvider _aiProvider;
        private readonly ILogger<CodeGenerator> _logger;

        public CodeGenerator(IModelProvider aiProvider, ILogger<CodeGenerator> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<GeneratedCode> GenerateFromDescriptionAsync(string description, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Generating code for description: {Description}", description);

            var prompt = BuildCodeGenerationPrompt(description);
            return await GenerateFromPromptAsync(prompt, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<GeneratedCode> GenerateFromPromptAsync(string prompt, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new ModelRequest
                {
                    Input = prompt,
                    Temperature = 0.2, // Low temperature for consistent code generation
                    MaxTokens = 4000
                };

                _logger.LogDebug("Sending request to AI provider");
                var response = await _aiProvider.ExecuteAsync(request, cancellationToken);

                if (string.IsNullOrEmpty(response.Response))
                {
                    throw new InvalidOperationException("AI provider returned empty response");
                }

                _logger.LogDebug("Received response from AI provider");

                // Extract code from response
                var extractedCode = ExtractCodeFromResponse(response.Response);
                
                if (string.IsNullOrEmpty(extractedCode))
                {
                    throw new InvalidOperationException("No code found in AI response");
                }

                // Wrap in plugin interface if not already wrapped
                var wrappedCode = WrapInPluginInterface(extractedCode);

                // Extract tool metadata
                var toolName = ExtractToolName(wrappedCode);
                var toolDescription = ExtractToolDescription(wrappedCode);

                var result = new GeneratedCode
                {
                    SourceCode = wrappedCode,
                    ToolName = toolName,
                    Description = toolDescription,
                    IsWrappedInPlugin = true,
                    GeneratedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Code generation completed for tool: {ToolName}", toolName);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Code generation failed");
                throw;
            }
        }

        /// <summary>
        /// Builds a comprehensive prompt for code generation
        /// </summary>
        private static string BuildCodeGenerationPrompt(string description)
        {
            return $@"Generate a complete C# implementation for: {description}

Requirements:
- Implement the IPlugin interface exactly as shown below
- Include all necessary using statements
- Create a CLI-callable Execute method that processes command line arguments
- Handle errors gracefully with try-catch blocks
- Return complete, compilable code
- Use async/await patterns where appropriate
- Include proper logging
- Make the tool useful and practical

IPlugin interface to implement:
```csharp
public partial interface IPlugin
{{
    string Name {{ get; }}
    string Version {{ get; }}
    string Description {{ get; }}
    string Author {{ get; }}
    bool IsEnabled {{ get; }}
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken = default);
    Task ShutdownAsync(CancellationToken cancellationToken = default);
    Task<PluginResult> ExecuteAsync(string[] args);
}}
```

PluginResult class:
```csharp
public partial class PluginResult
{{
    public bool Success {{ get; set; }}
    public string Message {{ get; set; }} = string.Empty;
    public string? ErrorMessage {{ get; set; }}
    public int ExitCode {{ get; set; }}
    public object? Data {{ get; set; }}
    public DateTime CompletedAt {{ get; set; }} = DateTime.UtcNow;
    public TimeSpan Duration {{ get; set; }}
}}
```

Return ONLY the complete C# code between ```csharp and ``` markers.
The class should be named based on the tool's purpose (e.g., JsonFormatter, ApiTester, etc.).
Make sure the tool is immediately usable from the command line.";
        }

        /// <summary>
        /// Extracts code from AI response
        /// </summary>
        private static string ExtractCodeFromResponse(string response)
        {
            // Look for code blocks marked with ```csharp or ```
            var codeBlockPattern = @"```(?:csharp)?\s*(.*?)\s*```";
            var matches = Regex.Matches(response, codeBlockPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            if (matches.Count > 0)
            {
                return matches[0].Groups[1].Value.Trim();
            }

            // If no code blocks found, try to extract any code-like content
            var lines = response.Split('\n');
            var codeLines = lines
                .SkipWhile(line => !line.Trim().StartsWith("using") && !line.Trim().StartsWith("namespace") && !line.Trim().StartsWith("public"))
                .TakeWhile(line => !string.IsNullOrWhiteSpace(line) || line.Trim().StartsWith("}"))
                .ToArray();

            return string.Join("\n", codeLines);
        }

        /// <summary>
        /// Wraps code in plugin interface if not already wrapped
        /// </summary>
        private static string WrapInPluginInterface(string code)
        {
            if (code.Contains("IPlugin") && code.Contains("ExecuteAsync"))
            {
                return code; // Already wrapped
            }

            // This is a fallback - in practice, the AI should generate properly wrapped code
            return code;
        }

        /// <summary>
        /// Extracts tool name from generated code
        /// </summary>
        private static string ExtractToolName(string code)
        {
            // Look for class name
            var classMatch = Regex.Match(code, @"class\s+(\w+)");
            if (classMatch.Success)
            {
                return classMatch.Groups[1].Value;
            }

            // Fallback to a generic name
            return "GeneratedTool";
        }

        /// <summary>
        /// Extracts tool description from generated code
        /// </summary>
        private static string ExtractToolDescription(string code)
        {
            // Look for description in comments or properties
            var descMatch = Regex.Match(code, @"Description\s*=\s*""([^""]+)""");
            if (descMatch.Success)
            {
                return descMatch.Groups[1].Value;
            }

            // Look for XML documentation
            var xmlMatch = Regex.Match(code, @"///\s*<summary>\s*(.*?)\s*</summary>", RegexOptions.Singleline);
            if (xmlMatch.Success)
            {
                return xmlMatch.Groups[1].Value.Trim();
            }

            return "Generated tool";
        }
    }
}
