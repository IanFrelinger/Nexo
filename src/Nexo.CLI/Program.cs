using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using System.Linq;
using System.IO;
using Nexo.Feature.Template.Interfaces;
using Nexo.Shared.Models;
using Nexo.Shared;
using Nexo.CLI.Commands;
using Nexo.CLI.Commands.Unity;
using Nexo.Infrastructure.Commands;

namespace Nexo.CLI
{
    /// <summary>
    /// Main entry point for the Nexo CLI application.
    /// </summary>
    public class Program
    {
            /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        // Build host
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddNexoCliServices(configuration);
            })
            .Build();

#if !EXCLUDE_AI
        // Configure AI providers
        host.Services.ConfigureAIProviders();
#endif

        var rootCommand = new RootCommand("Nexo - AI-Enhanced Development Environment Orchestration Platform");
            
            // Version command
            var versionCommand = new Command("version", "Display version information");
            versionCommand.SetHandler(() =>
            {
                Console.WriteLine("Nexo CLI v1.0.0");
                Console.WriteLine("AI-Enhanced Development Environment Orchestration Platform");
            });
            rootCommand.AddCommand(versionCommand);
            
            // Analyze command
            var analyzeCommand = new Command("analyze", "Analyze code for quality and architectural compliance");
            var pathArgument = new Argument<string>("path", "Path to file or directory to analyze");
            var outputOption = new Option<string>("--output", "Output format (json, text)") { IsRequired = false };
            var aiOption = new Option<bool>("--ai", "Use AI-enhanced analysis") { IsRequired = false };
            analyzeCommand.AddArgument(pathArgument);
            analyzeCommand.AddOption(outputOption);
            analyzeCommand.AddOption(aiOption);
            analyzeCommand.SetHandler(async (path, output, ai, provider, model) =>
            {
                using var scope = host.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                logger.LogInformation("Analyzing: {Path}", path);
                logger.LogInformation("Output format: {Output}", output ?? "text");
                logger.LogInformation("AI-enhanced: {AI}", ai);

                if (ai)
                {
                    var cachingProcessor = scope.ServiceProvider.GetRequiredService<CachingAsyncProcessor<ModelRequest, string, ModelResponse>>();
                    var aiSettings = scope.ServiceProvider.GetRequiredService<Nexo.Feature.AI.Models.AiSettings>();
                    
                    var effectiveProvider = !string.IsNullOrEmpty(provider) ? provider : aiSettings.PreferredProvider;
                    var effectiveModel = !string.IsNullOrEmpty(model) ? model : aiSettings.PreferredModel;

                    var request = new ModelRequest(0.9, 0.0, 0.0, false) { Input = $"Analyze: {path}" };
                    var response = await cachingProcessor.ProcessAsync(request);
                    Console.WriteLine("AI Analysis Result: " + response.Content);
                }
                else
                {
                    var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
                    var pipelineEngine = scope.ServiceProvider.GetRequiredService<IPipelineExecutionEngine>();
                    var pipelineConfigService = scope.ServiceProvider.GetRequiredService<IPipelineConfigurationService>();
                    
                    try
                    {
                        // Create analysis pipeline configuration
                        var analysisConfig = new PipelineConfiguration
                        {
                            Name = "Code Analysis Pipeline",
                            Version = "1.0.0",
                            Description = "Pipeline for analyzing code quality and structure",
                            Author = "Nexo CLI",
                            Tags = new List<string> { "analysis", "code-quality" },
                            Execution = new PipelineExecutionSettings
                            {
                                MaxParallelExecutions = 2,
                                CommandTimeoutMs = 30000,
                                EnableDetailedLogging = true,
                                EnablePerformanceMonitoring = true
                            },
                            Commands = new List<PipelineCommandConfiguration>
                            {
                                new PipelineCommandConfiguration
                                {
                                    Id = "analyze-code",
                                    Name = "Analyze Code",
                                    Description = "Analyzes code quality and structure",
                                    Category = "Analysis",
                                    Priority = "High",
                                    Parameters = new Dictionary<string, object>
                                    {
                                        { "path", path },
                                        { "output", output ?? "text" }
                                    }
                                }
                            }
                        };

                        // Execute the analysis pipeline
                        var result = await pipelineEngine.ExecuteAsync(pipelineContext, new List<string> { "analyze-code" });
                        
                        if (result.IsSuccess)
                        {
                            Console.WriteLine($"Analysis completed successfully. Execution time: {result.ExecutionTimeMs}ms");
                        }
                        else
                        {
                            Console.WriteLine($"Analysis failed: {result.ErrorMessage}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error executing analysis pipeline");
                        Console.WriteLine($"Analysis failed: {ex.Message}");
                    }
                }
            }, pathArgument, outputOption, aiOption, new Option<string>("--provider", "Preferred AI provider"), new Option<string>("--model", "Preferred AI model"));
            rootCommand.AddCommand(analyzeCommand);
            
#if !EXCLUDE_AI
            // AI commands
            var aiCommand = new Command("ai", "AI-powered development commands");
            
            var suggestCommand = new Command("suggest", "Get AI-powered code suggestions");
            var codeArgument = new Argument<string>("code", "Code to analyze");
            var contextOption = new Option<string>("--context", "Additional context") { IsRequired = false };
            var modelOption = new Option<string>("--model", "AI model to use") { IsRequired = false };
            suggestCommand.AddArgument(codeArgument);
            suggestCommand.AddOption(contextOption);
            suggestCommand.AddOption(modelOption);
            suggestCommand.SetHandler(async (code, context, modelOpt, providerOpt) =>
            {
                using var scope = host.Services.CreateScope();
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
            aiCommand.AddCommand(suggestCommand);
            
            var optimizeCommand = new Command("optimize", "Optimize code performance using AI");
            var fileArgument = new Argument<string>("file", "File to optimize");
            var levelOption = new Option<string>("--level", "Optimization level (basic, advanced, aggressive)") { IsRequired = false };
            var preserveOption = new Option<bool>("--preserve", "Preserve original file") { IsRequired = false };
            optimizeCommand.AddArgument(fileArgument);
            optimizeCommand.AddOption(levelOption);
            optimizeCommand.AddOption(preserveOption);
            optimizeCommand.SetHandler(async (file, level, preserve, provider, model) =>
            {
                using var scope = host.Services.CreateScope();
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
            aiCommand.AddCommand(optimizeCommand);
            
            var aiAnalyzeCommand = new Command("analyze", "AI-powered code analysis");
            var analyzePathArgument = new Argument<string>("path", "Path to analyze");
            var depthOption = new Option<string>("--depth", "Analysis depth (surface, deep, comprehensive)") { IsRequired = false };
            var focusOption = new Option<string>("--focus", "Focus areas (performance, security, quality)") { IsRequired = false };
            aiAnalyzeCommand.AddArgument(analyzePathArgument);
            aiAnalyzeCommand.AddOption(depthOption);
            aiAnalyzeCommand.AddOption(focusOption);
            aiAnalyzeCommand.SetHandler(async (path, depth, focus) =>
            {
                using var scope = host.Services.CreateScope();
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
            aiCommand.AddCommand(aiAnalyzeCommand);
            
            rootCommand.AddCommand(aiCommand);
#endif
            
            // Pipeline commands
            var pipelineCommand = new Command("pipeline", "Pipeline orchestration commands");
            
            var executeCommand = new Command("execute", "Execute a pipeline");
            var pipelineArgument = new Argument<string>("pipeline", "Pipeline configuration file or name");
            var dryRunOption = new Option<bool>("--dry-run", "Show what would be executed without running") { IsRequired = false };
            var modeOption = new Option<string>("--mode", "Execution mode (development, production, ai-heavy)") { IsRequired = false };
            executeCommand.AddArgument(pipelineArgument);
            executeCommand.AddOption(dryRunOption);
            executeCommand.AddOption(modeOption);
            executeCommand.SetHandler(async (pipeline, dryRun, mode) =>
            {
                using var scope = host.Services.CreateScope();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                var pipelineContext = scope.ServiceProvider.GetRequiredService<IPipelineContext>();
                
                logger.LogInformation("Executing pipeline: {Pipeline}", pipeline);
                logger.LogInformation("Execution mode: {Mode}", mode ?? "development");
                if (dryRun)
                {
                    logger.LogInformation("DRY RUN MODE - No actual execution");
                }
                
                var pipelineEngine = scope.ServiceProvider.GetRequiredService<IPipelineExecutionEngine>();
                var pipelineConfigService = scope.ServiceProvider.GetRequiredService<IPipelineConfigurationService>();
                
                try
                {
                    PipelineConfiguration pipelineConfig;
                    
                    // Load pipeline configuration
                    if (File.Exists(pipeline))
                    {
                        pipelineConfig = await pipelineConfigService.LoadFromFileAsync(pipeline, CancellationToken.None);
                    }
                    else
                    {
                        // Try to load from template
                        pipelineConfig = await pipelineConfigService.LoadFromTemplateAsync(pipeline, new Dictionary<string, object>(), CancellationToken.None);
                    }

                    if (dryRun)
                    {
                        // Validate configuration
                        var validationResult = await pipelineConfigService.ValidateAsync(pipelineConfig, CancellationToken.None);
                        if (validationResult.IsValid)
                        {
                            Console.WriteLine($"Pipeline validation successful. Would execute {pipelineConfig.Commands.Count} commands.");
                        }
                        else
                        {
                            Console.WriteLine($"Pipeline validation failed:");
                            foreach (var issue in validationResult.Issues)
                            {
                                Console.WriteLine($"  - {issue.Message} in {issue.Field}");
                            }
                        }
                    }
                    else
                    {
                        // Execute the pipeline
                        var aggregatorIds = pipelineConfig.Aggregators.Select(a => a.Id).ToList();
                        if (!aggregatorIds.Any())
                        {
                            // If no aggregators, create a simple execution with commands
                            var result = await pipelineEngine.ExecuteAsync(pipelineContext, new List<string>(), CancellationToken.None);
                            
                            if (result.IsSuccess)
                            {
                                Console.WriteLine($"Pipeline execution completed successfully. Execution time: {result.ExecutionTimeMs}ms");
                            }
                            else
                            {
                                Console.WriteLine($"Pipeline execution failed: {result.ErrorMessage}");
                            }
                        }
                        else
                        {
                            var result = await pipelineEngine.ExecuteAsync(pipelineContext, aggregatorIds, CancellationToken.None);
                            
                            if (result.IsSuccess)
                            {
                                Console.WriteLine($"Pipeline execution completed successfully. Execution time: {result.ExecutionTimeMs}ms");
                            }
                            else
                            {
                                Console.WriteLine($"Pipeline execution failed: {result.ErrorMessage}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error executing pipeline: {Pipeline}", pipeline);
                    Console.WriteLine($"Pipeline execution failed: {ex.Message}");
                }
            }, pipelineArgument, dryRunOption, modeOption);
            pipelineCommand.AddCommand(executeCommand);
            
            rootCommand.AddCommand(pipelineCommand);
            
            // Project commands are now handled by the enhanced ProjectCommands class

            // Phase 4: Development Acceleration Commands
            using var scope = host.Services.CreateScope();
            var developmentAccelerator = scope.ServiceProvider.GetRequiredService<IDevelopmentAccelerator>();
            var workflowExecutionService = scope.ServiceProvider.GetRequiredService<IWorkflowExecutionService>();
            var templateService = scope.ServiceProvider.GetRequiredService<ITemplateService>();
            var intelligentTemplateService = scope.ServiceProvider.GetRequiredService<IIntelligentTemplateService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            // Development acceleration commands
            var devCommand = DevelopmentCommands.CreateDevelopmentCommand(developmentAccelerator, workflowExecutionService, logger);
            rootCommand.AddCommand(devCommand);

            // Interactive development commands
            var interactiveCommand = InteractiveCommands.CreateInteractiveCommand(developmentAccelerator, logger);
            rootCommand.AddCommand(interactiveCommand);

            // Enhanced project commands
            var enhancedProjectCommand = ProjectCommands.CreateProjectCommand(templateService, intelligentTemplateService, logger);
            rootCommand.AddCommand(enhancedProjectCommand);

            // Configuration commands
            var aiConfigurationService = scope.ServiceProvider.GetRequiredService<IAiConfigurationService>();
            var configCommand = ConfigurationCommands.CreateConfigurationCommand(aiConfigurationService, logger);
            rootCommand.AddCommand(configCommand);

            // Standalone testing commands (prevents hanging)
            var standaloneTestingCommand = StandaloneTestRunner.CreateStandaloneTestCommand(logger);
            rootCommand.AddCommand(standaloneTestingCommand);

            // Web commands
            var webCodeGenerator = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebCodeGenerator>();
            var wasmOptimizer = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.Interfaces.IWebAssemblyOptimizer>();
            var generateWebCodeUseCase = scope.ServiceProvider.GetRequiredService<Nexo.Feature.Web.UseCases.GenerateWebCodeUseCase>();
            var webCommand = WebCommands.CreateWebCommand(webCodeGenerator, wasmOptimizer, generateWebCodeUseCase, logger);
            rootCommand.AddCommand(webCommand);

            // Feature Factory commands
            var featureFactoryCommand = FeatureFactoryCommands.CreateFeatureFactoryCommand(scope.ServiceProvider, logger);
            rootCommand.AddCommand(featureFactoryCommand);

            // Enhanced CLI commands
            var interactiveCLI = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Interactive.IInteractiveCLI>();
            var realTimeDashboard = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Dashboard.IRealTimeDashboard>();
            var interactiveHelpSystem = scope.ServiceProvider.GetRequiredService<Nexo.CLI.Help.IInteractiveHelpSystem>();
            
            var enhancedInteractiveCommand = EnhancedCLICommands.CreateEnhancedInteractiveCommand(interactiveCLI, realTimeDashboard, interactiveHelpSystem, logger);
            rootCommand.AddCommand(enhancedInteractiveCommand);
            
            var enhancedHelpCommand = EnhancedCLICommands.CreateEnhancedHelpCommand(interactiveHelpSystem, logger);
            rootCommand.AddCommand(enhancedHelpCommand);
            
            var enhancedDashboardCommand = EnhancedCLICommands.CreateEnhancedDashboardCommand(realTimeDashboard, logger);
            rootCommand.AddCommand(enhancedDashboardCommand);
            
            var enhancedStatusCommand = EnhancedCLICommands.CreateEnhancedStatusCommand(interactiveCLI, logger);
            rootCommand.AddCommand(enhancedStatusCommand);

            // Unity commands
            var unityCommand = UnityCommands.CreateUnityCommand(scope.ServiceProvider);
            rootCommand.AddCommand(unityCommand);

            // Game development commands
            var gameCommand = GameDevelopmentCommands.CreateGameDevelopmentCommand(scope.ServiceProvider);
            rootCommand.AddCommand(gameCommand);

            // Adaptation commands
            var adaptationCommand = AdaptationCommands.CreateAdaptationCommand(scope.ServiceProvider);
            rootCommand.AddCommand(adaptationCommand);

            // Agent management commands
            var agentManagementCommands = new AgentManagementCommands(scope.ServiceProvider, logger);
            var agentCommand = new Command("agents", "Manage specialized AI agents");
            agentCommand.AddCommand(agentManagementCommands.CreateAgentListCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentAnalyzeCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentPerformanceCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentTestCommand());
            agentCommand.AddCommand(agentManagementCommands.CreateAgentRegistryCommand());
            rootCommand.AddCommand(agentCommand);

            // Iteration commands
            var iterationCommands = new IterationCommands(scope.ServiceProvider, logger);
            var iterationCommand = new Command("iteration", "Iteration strategy analysis and optimization");
            iterationCommand.AddCommand(iterationCommands.CreateIterationAnalyzeCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationBenchmarkCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationGenerateCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationOptimizeCommand());
            iterationCommand.AddCommand(iterationCommands.CreateIterationRecommendationsCommand());
            rootCommand.AddCommand(iterationCommand);

            // Extension generation commands
            var extensionCommand = ExtensionCommands.CreateExtensionCommand(scope.ServiceProvider);
            rootCommand.AddCommand(extensionCommand);

            // LLama AI Chat and Model Management commands
            var chatCommand = new ChatCommand(scope.ServiceProvider, logger, scope.ServiceProvider.GetRequiredService<IModelOrchestrator>());
            var chatCommandRoot = chatCommand.CreateChatCommand();
            rootCommand.AddCommand(chatCommandRoot);

            var modelCommand = new ModelCommand(scope.ServiceProvider, logger, scope.ServiceProvider.GetRequiredService<IModelOrchestrator>());
            var modelCommandRoot = modelCommand.CreateModelCommand();
            rootCommand.AddCommand(modelCommandRoot);

            // Remove duplicate testing commands - using simple testing commands above

            rootCommand.Description = "Nexo CLI provides AI-enhanced development environment orchestration capabilities with interactive mode, real-time dashboards, intelligent suggestions, and Unity game development tools.";

            return await rootCommand.InvokeAsync(args);
        }
    }

    public class StubPipelineConfiguration : IPipelineConfiguration
    {
        public int MaxParallelExecutions => Constants.Limits.DefaultMaxParallelExecutions;
        public int CommandTimeoutMs => Constants.Timeouts.DefaultCommandTimeoutMs;
        public int BehaviorTimeoutMs => Constants.Timeouts.DefaultBehaviorTimeoutMs;
        public int AggregatorTimeoutMs => Constants.Timeouts.DefaultAggregatorTimeoutMs;
        public int MaxRetries => Constants.Retry.DefaultMaxRetries;
        public int RetryDelayMs => Constants.Timeouts.DefaultRetryDelayMs;
        public bool EnableDetailedLogging => false;
        public bool EnablePerformanceMonitoring => false;
        public bool EnableExecutionHistory => false;
        public int MaxExecutionHistoryEntries => Constants.Limits.DefaultMaxExecutionHistoryEntries;
        public bool EnableParallelExecution => false;
        public bool EnableDependencyResolution => false;
        public bool EnableResourceManagement => false;
        public long MaxMemoryUsageBytes => Constants.Limits.DefaultMaxMemoryUsageBytes;
        public double MaxCpuUsagePercentage => Constants.Limits.DefaultMaxCpuUsagePercentage;
        public T GetValue<T>(string key, T defaultValue = default(T)!) => defaultValue;
        public void SetValue<T>(string key, T value) { }
        public IEnumerable<string> GetKeys() => Array.Empty<string>();
        public bool HasKey(string key) => false;
    }
}
