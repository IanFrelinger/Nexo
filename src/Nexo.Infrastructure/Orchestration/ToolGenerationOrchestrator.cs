using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Interfaces.Extensions;
using Nexo.Core.Domain.Interfaces;
using Nexo.Core.Domain.Models;
using Nexo.Core.Domain.Models.CodeQuality;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Safety;

namespace Nexo.Infrastructure.Orchestration
{
    /// <summary>
    /// Orchestrates the complete tool generation pipeline
    /// </summary>
    public partial class ToolGenerationOrchestrator
    {
        private readonly IModelProvider _aiProvider;
        private readonly ICodeGenerator _codeGenerator;
        private readonly ICompilationService _compiler;
        private readonly IPluginLoader _pluginLoader;
        private readonly IToolRepository _toolRepo;
        private readonly ICodeQualityAnalyzer _qualityAnalyzer;
        private readonly EnhancedSafetyValidator _safetyValidator;
        private readonly ILogger<ToolGenerationOrchestrator> _logger;

        public ToolGenerationOrchestrator(
            IModelProvider aiProvider,
            ICodeGenerator codeGenerator,
            ICompilationService compiler,
            IPluginLoader pluginLoader,
            IToolRepository toolRepo,
            ICodeQualityAnalyzer qualityAnalyzer,
            EnhancedSafetyValidator safetyValidator,
            ILogger<ToolGenerationOrchestrator> logger)
        {
            _aiProvider = aiProvider ?? throw new ArgumentNullException(nameof(aiProvider));
            _codeGenerator = codeGenerator ?? throw new ArgumentNullException(nameof(codeGenerator));
            _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _toolRepo = toolRepo ?? throw new ArgumentNullException(nameof(toolRepo));
            _qualityAnalyzer = qualityAnalyzer ?? throw new ArgumentNullException(nameof(qualityAnalyzer));
            _safetyValidator = safetyValidator ?? throw new ArgumentNullException(nameof(safetyValidator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Generates a complete tool from a natural language description
        /// </summary>
        /// <param name="description">Natural language description of the desired tool</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Generated tool ready for execution</returns>
        public async Task<GeneratedTool> GenerateToolAsync(string description, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting tool generation for: {Description}", description);

            try
            {
                // Step 1: Generate code from description
                _logger.LogDebug("Step 1: Generating code from description");
                var generatedCode = await _codeGenerator.GenerateFromDescriptionAsync(description, cancellationToken);
                
                if (string.IsNullOrEmpty(generatedCode.SourceCode))
                {
                    throw new InvalidOperationException("Failed to generate code from description");
                }

                _logger.LogDebug("Generated code for tool: {ToolName}", generatedCode.ToolName);

                // Step 2: Validate code safety
                _logger.LogDebug("Step 2: Validating code safety");
                var safetyResult = await _safetyValidator.ValidateGeneratedCodeAsync(generatedCode.SourceCode, cancellationToken);
                
                if (safetyResult.IsBlocked)
                {
                    _logger.LogError("Code generation blocked due to safety issues");
                    throw new InvalidOperationException($"Code generation blocked due to safety issues: {string.Join(", ", safetyResult.Issues.Select(i => i.Message))}");
                }
                
                if (safetyResult.RequiresHumanReview)
                {
                    _logger.LogWarning("Code requires human review due to safety concerns");
                    // In a real implementation, this would trigger a human review workflow
                }

                _logger.LogInformation("Safety validation completed. Valid: {IsValid}, Issues: {IssueCount}", 
                    safetyResult.IsValid, safetyResult.Issues.Count);

                // Step 3: Analyze code quality
                _logger.LogDebug("Step 3: Analyzing code quality");
                var qualityScore = await _qualityAnalyzer.AnalyzeCodeAsync(generatedCode.SourceCode, generatedCode.ToolName, cancellationToken);
                
                _logger.LogInformation("Code quality analysis completed. Overall Score: {Score}, Quality Level: {Level}", 
                    qualityScore.OverallScore, qualityScore.QualityLevel);

                // Check quality gates
                if (!_qualityAnalyzer.PassesQualityGate(qualityScore))
                {
                    _logger.LogWarning("Code quality below threshold. Score: {Score}, Required: 6.0", qualityScore.OverallScore);
                    
                    if (_qualityAnalyzer.RequiresHumanReview(qualityScore))
                    {
                        _logger.LogWarning("Code requires human review due to quality issues");
                        // In a real implementation, this would trigger a human review workflow
                    }
                }

                // Step 4: Compile to assembly
                _logger.LogDebug("Step 4: Compiling code to assembly");
                var compilationResult = await _compiler.CompileToAssemblyAsync(generatedCode.SourceCode, cancellationToken);
                
                if (!compilationResult.Success)
                {
                    _logger.LogWarning("Initial compilation failed, attempting to fix errors");
                    
                    // Try to fix compilation errors
                    compilationResult = await _compiler.FixAndCompileAsync(
                        generatedCode.SourceCode, 
                        compilationResult.Errors, 
                        cancellationToken);
                    
                    if (!compilationResult.Success)
                    {
                        throw new InvalidOperationException($"Compilation failed: {string.Join(", ", compilationResult.Errors)}");
                    }
                }

                _logger.LogDebug("Code compiled successfully");

                // Step 5: Load plugin from assembly
                _logger.LogDebug("Step 5: Loading plugin from assembly");
                if (compilationResult.Assembly == null)
                {
                    return new GeneratedTool
                    {
                        Name = generatedCode.ToolName,
                        IsReady = false,
                        Description = "Compilation succeeded but no assembly was generated"
                    };
                }
                
                var pluginLoadResult = await _pluginLoader.LoadPluginAsync(compilationResult.Assembly, generatedCode.ToolName);
                
                if (!pluginLoadResult.IsSuccess || pluginLoadResult.Plugin == null)
                {
                    var errorMessage = pluginLoadResult.HasErrors 
                        ? string.Join(", ", pluginLoadResult.Errors.Select(e => e.Message))
                        : "Unknown error loading plugin";
                    throw new InvalidOperationException($"Failed to load plugin: {errorMessage}");
                }

                _logger.LogDebug("Plugin loaded successfully: {PluginName}", pluginLoadResult.Plugin.Name);

                // Step 6: Persist tool
                _logger.LogDebug("Step 6: Persisting tool");
                var savedTool = await _toolRepo.SaveToolAsync(
                    pluginLoadResult.Plugin, 
                    compilationResult.Assembly!, 
                    compilationResult.FixedSourceCode ?? generatedCode.SourceCode, 
                    cancellationToken);

                _logger.LogDebug("Tool persisted with ID: {ToolId}", savedTool.Id);

                // Step 7: Create and return usable tool
                var generatedTool = new GeneratedTool
                {
                    Plugin = pluginLoadResult.Plugin,
                    Name = generatedCode.ToolName,
                    Description = generatedCode.Description,
                    CommandName = GenerateCommandName(generatedCode.ToolName),
                    GeneratedAt = DateTime.UtcNow,
                    IsReady = true,
                    QualityScore = qualityScore
                };

                _logger.LogInformation("Tool generation completed successfully: {ToolName}", generatedTool.Name);
                return generatedTool;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool generation failed for: {Description}", description);
                throw;
            }
        }

        /// <summary>
        /// Generates a command name from a tool name
        /// </summary>
        /// <param name="toolName">Tool name</param>
        /// <returns>Command name for CLI usage</returns>
        private static string GenerateCommandName(string toolName)
        {
            // Convert tool name to kebab-case for CLI usage
            return toolName.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Replace(".", "-");
        }
    }
}
