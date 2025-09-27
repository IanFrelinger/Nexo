using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;

namespace Nexo.CLI.Program.Commands
{
    /// <summary>
    /// Handles pipeline-related command creation and execution
    /// </summary>
    public partial class PipelineCommandHandler
    {
        private readonly IServiceProvider _serviceProvider;

        public PipelineCommandHandler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Command CreatePipelineCommand()
        {
            var pipelineCommand = new Command("pipeline", "Pipeline orchestration commands");

            // Execute command
            var executeCommand = CreateExecuteCommand();
            pipelineCommand.AddCommand(executeCommand);

            // Pipeline run command
            var pipelineRunCommand = CreatePipelineRunCommand();
            pipelineCommand.AddCommand(pipelineRunCommand);

            return pipelineCommand;
        }

        private Command CreateExecuteCommand()
        {
            var executeCommand = new Command("execute", "Execute a pipeline");
            var pipelineArgument = new Argument<string>("pipeline", "Pipeline configuration file or name");
            var dryRunOption = new Option<bool>("--dry-run", "Show what would be executed without running") { IsRequired = false };
            var modeOption = new Option<string>("--mode", "Execution mode (development, production, ai-heavy)") { IsRequired = false };
            executeCommand.AddArgument(pipelineArgument);
            executeCommand.AddOption(dryRunOption);
            executeCommand.AddOption(modeOption);
            executeCommand.SetHandler(async (pipeline, dryRun, mode) =>
            {
                using var scope = _serviceProvider.CreateScope();
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
            return executeCommand;
        }

        private Command CreatePipelineRunCommand()
        {
            var pipelineRunCommand = PipelineRunCommand.CreateCommand(_serviceProvider, _serviceProvider.GetRequiredService<ILogger<PipelineRunCommand>>());
            return pipelineRunCommand;
        }
    }
}
