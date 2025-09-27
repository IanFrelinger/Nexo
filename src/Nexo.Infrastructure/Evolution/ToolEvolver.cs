using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;

namespace Nexo.Infrastructure.Evolution
{
    /// <summary>
    /// Evolves and modifies existing tools using AI
    /// </summary>
    public partial class ToolEvolver : IToolEvolver
    {
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICompilationService _compiler;
        private readonly IPluginLoader _pluginLoader;
        private readonly IToolRepository _toolRepo;
        private readonly ILogger<ToolEvolver> _logger;

        public ToolEvolver(
            ICodeGenerator codeGenerator,
            ICompilationService compiler,
            IPluginLoader pluginLoader,
            IToolRepository toolRepo,
            ILogger<ToolEvolver> logger)
        {
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _toolRepo = toolRepo ?? throw new ArgumentNullException(nameof(toolRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task<EvolvedTool> EvolveToolAsync(string toolName, string modification, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Evolving tool: {ToolName} with modification: {Modification}", toolName, modification);

            try
            {
                // Step 1: Load existing tool source
                var existingCode = await _toolRepo.LoadToolSourceAsync(toolName, cancellationToken);
                if (string.IsNullOrEmpty(existingCode))
                {
                    throw new InvalidOperationException($"Tool source not found: {toolName}");
                }

                _logger.LogDebug("Loaded existing source for tool: {ToolName}", toolName);

                // Step 2: Create evolution prompt
                var evolutionPrompt = BuildEvolutionPrompt(existingCode, modification);
                
                // Step 3: Generate evolved version
                var evolvedCode = await _codeGenerator.GenerateFromPromptAsync(evolutionPrompt, cancellationToken);
                
                if (string.IsNullOrEmpty(evolvedCode.SourceCode))
                {
                    throw new InvalidOperationException("Failed to generate evolved code");
                }

                _logger.LogDebug("Generated evolved code for tool: {ToolName}", toolName);

                // Step 4: Compile evolved version
                var compilationResult = await _compiler.CompileToAssemblyAsync(evolvedCode.SourceCode, cancellationToken);
                
                if (!compilationResult.Success)
                {
                    _logger.LogWarning("Evolved code compilation failed, attempting to fix errors");
                    
                    compilationResult = await _compiler.FixAndCompileAsync(
                        evolvedCode.SourceCode, 
                        compilationResult.Errors, 
                        cancellationToken);
                    
                    if (!compilationResult.Success)
                    {
                        return new EvolvedTool
                        {
                            Name = toolName,
                            EvolutionDescription = modification,
                            Success = false,
                            Errors = compilationResult.Errors
                        };
                    }
                }

                _logger.LogDebug("Evolved code compiled successfully for tool: {ToolName}", toolName);

                // Step 5: Get current version and increment
                var currentTool = await _toolRepo.GetToolAsync(toolName, cancellationToken);
                var newVersion = (currentTool?.Version ?? 1) + 1;

                // Step 6: Load evolved plugin
                if (compilationResult.Assembly == null)
                {
                    return new EvolvedTool
                    {
                        Name = toolName,
                        Version = newVersion,
                        EvolutionDescription = modification,
                        Success = false,
                        Errors = new[] { "Compilation succeeded but no assembly was generated" }
                    };
                }
                
                var pluginLoadResult = await _pluginLoader.LoadPluginAsync(compilationResult.Assembly, toolName);
                
                if (!pluginLoadResult.IsSuccess || pluginLoadResult.Plugin == null)
                {
                    var errorMessage = pluginLoadResult.HasErrors 
                        ? string.Join(", ", pluginLoadResult.Errors.Select(e => e.Message))
                        : "Unknown error loading evolved plugin";
                    return new EvolvedTool
                    {
                        Name = toolName,
                        EvolutionDescription = modification,
                        Success = false,
                        Errors = new[] { $"Failed to load evolved plugin: {errorMessage}" }
                    };
                }

                _logger.LogDebug("Evolved plugin loaded successfully for tool: {ToolName}", toolName);

                // Step 7: Save new version
                await _toolRepo.SaveVersionAsync(toolName, compilationResult.FixedSourceCode ?? evolvedCode.SourceCode, newVersion, cancellationToken);

                _logger.LogInformation("Tool evolution completed successfully: {ToolName} v{Version}", toolName, newVersion);

                return new EvolvedTool
                {
                    Name = toolName,
                    Version = newVersion,
                    EvolutionDescription = modification,
                    Success = true,
                    EvolvedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool evolution failed for: {ToolName}", toolName);
                return new EvolvedTool
                {
                    Name = toolName,
                    EvolutionDescription = modification,
                    Success = false,
                    Errors = new[] { ex.Message }
                };
            }
        }

        /// <inheritdoc />
        public async Task<bool> CanEvolveToolAsync(string toolName, CancellationToken cancellationToken = default)
        {
            try
            {
                var tool = await _toolRepo.GetToolAsync(toolName, cancellationToken);
                return tool != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if tool can be evolved: {ToolName}", toolName);
                return false;
            }
        }

        /// <summary>
        /// Builds a prompt for tool evolution
        /// </summary>
        private static string BuildEvolutionPrompt(string existingCode, string modification)
        {
            return $@"Modify this existing tool to: {modification}

Current implementation:
```csharp
{existingCode}
```

Requirements:
- Preserve all existing functionality
- Add the requested modification
- Maintain the IPlugin interface implementation
- Keep all existing using statements
- Ensure the code compiles without errors
- Return the COMPLETE modified code
- Do not remove any existing features unless explicitly requested

Return ONLY the complete modified code between ```csharp and ``` markers.";
        }
    }
}
