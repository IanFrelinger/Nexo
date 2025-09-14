using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Logging;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Models;

namespace Nexo.Infrastructure.Compilation
{
    /// <summary>
    /// Compiles C# code to assemblies using Roslyn
    /// </summary>
    public class RoslynCompilationService : ICompilationService
    {
        private readonly IModelProvider _aiProvider;
        private readonly ILogger<RoslynCompilationService> _logger;
        private readonly List<MetadataReference> _references;

        public RoslynCompilationService(IModelProvider aiProvider, ILogger<RoslynCompilationService> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _references = BuildMetadataReferences();
        }

        /// <inheritdoc />
        public async Task<CompilationResult> CompileToAssemblyAsync(string code, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting compilation of generated code");

            try
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                var compilation = CSharpCompilation.Create(
                    $"GeneratedPlugin_{Guid.NewGuid()}",
                    new[] { syntaxTree },
                    _references,
                    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                        .WithOptimizationLevel(OptimizationLevel.Release)
                        .WithPlatform(Platform.AnyCpu)
                );

                using var ms = new MemoryStream();
                var result = compilation.Emit(ms);

                if (result.Success)
                {
                    _logger.LogInformation("Compilation successful");
                    return new CompilationResult
                    {
                        Success = true,
                        Assembly = ms.ToArray(),
                        CompiledAt = DateTime.UtcNow
                    };
                }

                _logger.LogWarning("Compilation failed with {ErrorCount} errors", result.Diagnostics.Count(d => d.Severity == DiagnosticSeverity.Error));
                
                var errors = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString())
                    .ToArray();

                var warnings = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Warning)
                    .Select(d => d.ToString())
                    .ToArray();

                return new CompilationResult
                {
                    Success = false,
                    Errors = errors,
                    Warnings = warnings,
                    CompiledAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Compilation failed with exception");
                return new CompilationResult
                {
                    Success = false,
                    Errors = new[] { ex.Message },
                    CompiledAt = DateTime.UtcNow
                };
            }
        }

        /// <inheritdoc />
        public async Task<CompilationResult> FixAndCompileAsync(string code, string[] errors, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Attempting to fix compilation errors using AI");

            try
            {
                var fixPrompt = BuildFixPrompt(code, errors);
                var request = new ModelRequest
                {
                    Input = fixPrompt,
                    Temperature = 0.1, // Very low temperature for fixing code
                    MaxTokens = 4000
                };

                var response = await _aiProvider.ExecuteAsync(request, cancellationToken);
                
                if (string.IsNullOrEmpty(response.Response))
                {
                    throw new InvalidOperationException("AI provider returned empty response for code fix");
                }

                var fixedCode = ExtractFixedCode(response.Response);
                
                if (string.IsNullOrEmpty(fixedCode))
                {
                    _logger.LogWarning("Could not extract fixed code from AI response");
                    return new CompilationResult
                    {
                        Success = false,
                        Errors = errors,
                        CompiledAt = DateTime.UtcNow
                    };
                }

                _logger.LogInformation("Retrying compilation with fixed code");
                return await CompileToAssemblyAsync(fixedCode, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix compilation errors");
                return new CompilationResult
                {
                    Success = false,
                    Errors = errors.Concat(new[] { $"Fix attempt failed: {ex.Message}" }).ToArray(),
                    CompiledAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Builds metadata references for compilation
        /// </summary>
        private static List<MetadataReference> BuildMetadataReferences()
        {
            var references = new List<MetadataReference>();

            // Core system assemblies
            var coreAssemblies = new[]
            {
                typeof(object).Assembly,
                typeof(Task).Assembly,
                typeof(Console).Assembly,
                typeof(System.IO.File).Assembly,
                typeof(System.Text.Json.JsonSerializer).Assembly,
                typeof(Microsoft.Extensions.Logging.ILogger).Assembly,
                typeof(System.Collections.Generic.List<>).Assembly
            };

            foreach (var assembly in coreAssemblies)
            {
                try
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {
                    // Skip assemblies that can't be referenced
                }
            }

            // Add Nexo assemblies
            var nexoAssemblies = new[]
            {
                "Nexo.Core.Domain",
                "Nexo.Core.Application"
            };

            foreach (var assemblyName in nexoAssemblies)
            {
                try
                {
                    var assembly = Assembly.Load(assemblyName);
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
                catch
                {
                    // Skip if assembly not found
                }
            }

            return references;
        }

        /// <summary>
        /// Builds a prompt for fixing compilation errors
        /// </summary>
        private static string BuildFixPrompt(string code, string[] errors)
        {
            return $@"Fix the following C# compilation errors in this code:

Errors:
{string.Join("\n", errors)}

Code:
```csharp
{code}
```

Requirements:
- Fix all compilation errors
- Maintain the original functionality
- Keep the IPlugin interface implementation
- Return ONLY the complete fixed code between ```csharp and ``` markers
- Ensure the code compiles without errors
- Preserve all using statements and namespace declarations";
        }

        /// <summary>
        /// Extracts fixed code from AI response
        /// </summary>
        private static string ExtractFixedCode(string response)
        {
            // Look for code blocks marked with ```csharp or ```
            var codeBlockPattern = @"```(?:csharp)?\s*(.*?)\s*```";
            var matches = System.Text.RegularExpressions.Regex.Matches(response, codeBlockPattern, System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (matches.Count > 0)
            {
                return matches[0].Groups[1].Value.Trim();
            }

            return string.Empty;
        }
    }
}
