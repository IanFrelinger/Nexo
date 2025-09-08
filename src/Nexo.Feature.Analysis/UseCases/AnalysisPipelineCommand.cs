using Microsoft.Extensions.Logging;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Models;
using Nexo.Feature.Analysis.Enums;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Enums;
using Nexo.Feature.Pipeline.Models;
using Nexo.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Nexo.Feature.Analysis.UseCases
{
    /// <summary>
    /// Pipeline command for performing comprehensive code analysis.
    /// Integrates with the pipeline architecture to execute analysis workflows.
    /// </summary>
    public class AnalysisPipelineCommand : ICommand
    {
        private readonly ILogger<AnalysisPipelineCommand> _logger;
        private readonly ICodeAnalyzer _codeAnalyzer;
        private readonly IArchitectureAnalyzer _architectureAnalyzer;

        public string Id => "analysis-pipeline-command";
        public string Name => "Analysis Pipeline Command";
        public string Description => "Performs comprehensive code analysis including code quality and architecture analysis";
        public CommandCategory Category => CommandCategory.Analysis;
        public IReadOnlyList<string> Tags => new List<string> { "analysis", "code-quality", "architecture", "pipeline" };
        public CommandPriority Priority => CommandPriority.Normal;
        public bool CanExecuteInParallel => false;
        public IReadOnlyList<CommandDependency> Dependencies => new List<CommandDependency>();

        public AnalysisPipelineCommand(
            ILogger<AnalysisPipelineCommand> logger,
            ICodeAnalyzer codeAnalyzer,
            IArchitectureAnalyzer architectureAnalyzer)
        {
            _logger = logger;
            _codeAnalyzer = codeAnalyzer;
            _architectureAnalyzer = architectureAnalyzer;
        }

        public Task<CommandValidationResult> ValidateAsync(IPipelineContext context)
        {
            try
            {
                _logger.LogInformation("Validating AnalysisPipelineCommand");

                if (context == null)
                {
                    return Task.FromResult(CommandValidationResult.Invalid("Pipeline context is required"));
                }

                // Check if analysis request is available in context
                if (!context.HasValue("AnalysisRequest"))
                {
                    return Task.FromResult(CommandValidationResult.Invalid("AnalysisRequest not found in pipeline context"));
                }

                var request = context.GetValue<AnalysisRequest>("AnalysisRequest");
                if (request == null)
                {
                    return Task.FromResult(CommandValidationResult.Invalid("Invalid AnalysisRequest in pipeline context"));
                }

                if (string.IsNullOrEmpty(request.TargetPath))
                {
                    return Task.FromResult(CommandValidationResult.Invalid("Target path is required for analysis"));
                }

                _logger.LogInformation("AnalysisPipelineCommand validation successful");
                return Task.FromResult(CommandValidationResult.Valid());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating AnalysisPipelineCommand");
                return Task.FromResult(CommandValidationResult.Invalid($"Validation error: {ex.Message}"));
            }
        }

        public Task<Nexo.Feature.Pipeline.Models.CommandResult> ExecuteAsync(IPipelineContext context)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                _logger.LogInformation("Executing AnalysisPipelineCommand");

                var request = context.GetValue<AnalysisRequest>("AnalysisRequest");
                if (request == null)
                {
                    return Task.FromResult(Nexo.Feature.Pipeline.Models.CommandResult.Failure("AnalysisRequest not found in context", null, 0, DateTime.UtcNow, DateTime.UtcNow));
                }
                
                var results = new List<string>();

                // Perform code analysis
                if (!string.IsNullOrEmpty(request.Code))
                {
                    _logger.LogInformation("Performing code analysis");
                    var codeResult = _codeAnalyzer.AnalyzeCode(request.Code);
                    results.Add($"Code Analysis: {codeResult}");
                }

                // Perform architecture analysis
                if (!string.IsNullOrEmpty(request.Code))
                {
                    _logger.LogInformation("Performing architecture analysis");
                    var archResult = _architectureAnalyzer.AnalyzeArchitecture(request.Code);
                    results.Add($"Architecture Analysis: {archResult}");
                }

                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;

                // Store results in context for other pipeline components
                context.SetValue("AnalysisResults", results);

                _logger.LogInformation("AnalysisPipelineCommand executed successfully in {ExecutionTime}ms", executionTime);

                return Task.FromResult(Nexo.Feature.Pipeline.Models.CommandResult.Success(results, executionTime, startTime, endTime)
                    .AddInformation($"Analyzed {results.Count} aspects of the codebase"));
            }
            catch (Exception ex)
            {
                var endTime = DateTime.UtcNow;
                var executionTime = (long)(endTime - startTime).TotalMilliseconds;
                
                _logger.LogError(ex, "Error executing AnalysisPipelineCommand");
                return Task.FromResult(Nexo.Feature.Pipeline.Models.CommandResult.Failure($"Analysis execution failed: {ex.Message}", ex, executionTime, startTime, endTime));
            }
        }

        public Task CleanupAsync(IPipelineContext context)
        {
            try
            {
                _logger.LogInformation("Cleaning up AnalysisPipelineCommand");
                
                // Clean up any temporary files or resources
                if (context.HasValue("AnalysisResults"))
                {
                    context.RemoveValue("AnalysisResults");
                }
                
                _logger.LogInformation("AnalysisPipelineCommand cleanup completed");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AnalysisPipelineCommand cleanup");
                return Task.CompletedTask;
            }
        }

        public CommandMetadata GetMetadata()
        {
            return new CommandMetadata
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Category = Category,
                Tags = Tags.ToList(),
                Priority = Priority,
                CanExecuteInParallel = CanExecuteInParallel,
                Dependencies = Dependencies.ToList(),
                Version = "1.0.0",
                Author = "Nexo Analysis Team"
            };
        }
    }
} 