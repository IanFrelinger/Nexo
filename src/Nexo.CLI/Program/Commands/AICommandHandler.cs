using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Models;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.CLI.Program.Commands
{
    /// <summary>
    /// Handles AI-related command creation and execution
    /// </summary>
    public partial class AICommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public AICommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Command CreateAICommand()
        {
            var aiCommand = new Command("ai", "AI-powered development commands");

            // Suggest command
            var suggestCommand = CreateSuggestCommand();
            aiCommand.AddCommand(suggestCommand);

            // Optimize command
            var optimizeCommand = CreateOptimizeCommand();
            aiCommand.AddCommand(optimizeCommand);

            // AI Analyze command
            var aiAnalyzeCommand = CreateAIAnalyzeCommand();
            aiCommand.AddCommand(aiAnalyzeCommand);

            return aiCommand;
        }

        private Command CreateSuggestCommand()
        {
            var suggestCommand = new Command("suggest", "Get AI-powered code suggestions");
            var codeArgument = new Argument<string>("code", "Code to analyze");
            var contextOption = new Option<string>("--context", "Additional context") { IsRequired = false };
            var modelOption = new Option<string>("--model", "AI model to use") { IsRequired = false };
            suggestCommand.AddArgument(codeArgument);
            suggestCommand.AddOption(contextOption);
            suggestCommand.AddOption(modelOption);
            suggestCommand.SetHandler(async (code, context, modelOpt, providerOpt) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var cachingProcessor = scope.ServiceProvider.GetRequiredService<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>();
                var aiSettings = scope.ServiceProvider.GetRequiredService<Nexo.Feature.AI.Models.AiSettings>();

                logger.LogInformation("Getting AI suggestions for code of length: {Length}", code.Length);
                if (!string.IsNullOrEmpty(context))
                {
                    logger.LogInformation("Context provided: {Context}", context);
                }

                var effectiveProvider = !string.IsNullOrEmpty(providerOpt) ? providerOpt : aiSettings.PreferredProvider;
                var effectiveModel = !string.IsNullOrEmpty(modelOpt) ? modelOpt : aiSettings.PreferredModel;

                if (!string.IsNullOrEmpty(effectiveProvider))
                {
                    logger.LogInformation("Using provider: {Provider}", effectiveProvider);
                }
                if (!string.IsNullOrEmpty(effectiveModel))
                {
                    logger.LogInformation("Using model: {Model}", effectiveModel);
                }

                var request = new ModelRequest(0.9, 0.0, 0.0, false) { Input = code };
                var response = await cachingProcessor.ProcessAsync(request);
                Console.WriteLine("AI Suggestion: " + response.Content);
            }, codeArgument, contextOption, new Option<string>("--model", "Preferred AI model"), new Option<string>("--provider", "Preferred AI provider"));
            return suggestCommand;
        }

        private Command CreateOptimizeCommand()
        {
            var optimizeCommand = new Command("optimize", "Optimize code performance using AI");
            var fileArgument = new Argument<string>("file", "File to optimize");
            var levelOption = new Option<string>("--level", "Optimization level (basic, advanced, aggressive)") { IsRequired = false };
            var preserveOption = new Option<bool>("--preserve", "Preserve original file") { IsRequired = false };
            optimizeCommand.AddArgument(fileArgument);
            optimizeCommand.AddOption(levelOption);
            optimizeCommand.AddOption(preserveOption);
            optimizeCommand.SetHandler(async (file, level, preserve, provider, model) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var cachingProcessor = scope.ServiceProvider.GetRequiredService<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>();
                var aiSettings = scope.ServiceProvider.GetRequiredService<Nexo.Feature.AI.Models.AiSettings>();

                logger.LogInformation("Optimizing: {File}", file);
                logger.LogInformation("Optimization level: {Level}", level ?? "basic");
                logger.LogInformation("Preserve original: {Preserve}", preserve);

                var effectiveProvider = !string.IsNullOrEmpty(provider) ? provider : aiSettings.PreferredProvider;
                var effectiveModel = !string.IsNullOrEmpty(model) ? model : aiSettings.PreferredModel;

                var request = new ModelRequest(0.9, 0.0, 0.0, false) { Input = $"Optimize: {file}" };
                var response = await cachingProcessor.ProcessAsync(request);
                Console.WriteLine("AI Optimization Result: " + response.Content);
            }, fileArgument, levelOption, preserveOption, new Option<string>("--provider", "Preferred AI provider"), new Option<string>("--model", "Preferred AI model"));
            return optimizeCommand;
        }

        private Command CreateAIAnalyzeCommand()
        {
            var aiAnalyzeCommand = new Command("analyze", "AI-powered code analysis");
            var analyzePathArgument = new Argument<string>("path", "Path to analyze");
            var depthOption = new Option<string>("--depth", "Analysis depth (surface, deep, comprehensive)") { IsRequired = false };
            var focusOption = new Option<string>("--focus", "Focus areas (performance, security, quality)") { IsRequired = false };
            aiAnalyzeCommand.AddArgument(analyzePathArgument);
            aiAnalyzeCommand.AddOption(depthOption);
            aiAnalyzeCommand.AddOption(focusOption);
            aiAnalyzeCommand.SetHandler(async (path, depth, focus) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();

                logger.LogInformation("AI Analysis: {Path}", path);
                logger.LogInformation("Analysis depth: {Depth}", depth ?? "surface");
                logger.LogInformation("Focus areas: {Focus}", focus ?? "all");

                var pipelineEngine = scope.ServiceProvider.GetRequiredService<IPipelineExecutionEngine>();
                var pipelineConfigService = scope.ServiceProvider.GetRequiredService<IPipelineConfigurationService>();

                try
                {
                    // Create AI analysis pipeline configuration
                    var aiAnalysisConfig = new PipelineConfiguration
                    {
                        Name = "AI Code Analysis Pipeline",
                        Version = "1.0.0",
                        Description = "AI-powered code analysis pipeline",
                        Author = "Nexo CLI",
                        Tags = new List<string> { "ai", "analysis", "code-quality" },
                        Execution = new PipelineExecutionSettings
                        {
                            MaxParallelExecutions = 1,
                            CommandTimeoutMs = 60000,
                            EnableDetailedLogging = true,
                            EnablePerformanceMonitoring = true
                        },
                        Commands = new List<PipelineCommandConfiguration>
                        {
                            new PipelineCommandConfiguration
                            {
                                Id = "ai-analyze-code",
                                Name = "AI Code Analysis",
                                Description = "Performs AI-powered code analysis",
                                Category = "AI Analysis",
                                Priority = "High",
                                Parameters = new Dictionary<string, object>
                                {
                                    { "path", path },
                                    { "depth", depth ?? "surface" },
                                    { "focus", focus ?? "all" }
                                }
                            }
                        }
                    };

                    // Execute the AI analysis pipeline
                    var result = await pipelineEngine.ExecuteAsync(pipelineContext, new List<string> { "ai-analyze-code" });

                    if (result.IsSuccess)
                    {
                        Console.WriteLine($"AI analysis completed successfully. Execution time: {result.ExecutionTimeMs}ms");
                    }
                    else
                    {
                        Console.WriteLine($"AI analysis failed: {result.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing AI analysis pipeline");
                    Console.WriteLine($"AI analysis failed: {ex.Message}");
                }
            }, analyzePathArgument, depthOption, focusOption);
            return aiAnalyzeCommand;
        }
    }
}
