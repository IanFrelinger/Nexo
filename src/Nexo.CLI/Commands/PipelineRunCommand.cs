using System;
using System.CommandLine;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Pipeline;
using Nexo.Core.Contracts;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Interfaces;
using YamlDotNet.Serialization;

namespace Nexo.CLI.Commands
{
    /// <summary>
    /// Command handler for the pipeline run command.
    /// </summary>
    public partial class PipelineRunCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PipelineRunCommand> _logger;

        public PipelineRunCommand(IServiceProvider serviceProvider, ILogger<PipelineRunCommand> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates the pipeline run command with all options.
        /// </summary>
        public static Command CreateCommand(IServiceProvider serviceProvider, ILogger<PipelineRunCommand> logger)
        {
            var command = new Command("run", "Execute a pipeline with a request file or stdin input");

            var requestOption = new Option<FileInfo?>("--request", "Path to JSON or YAML request file")
            {
                IsRequired = false
            };
            requestOption.AddAlias("-r");

            var stdinOption = new Option<bool>("--stdin", "Read request from stdin")
            {
                IsRequired = false
            };

            var dryRunOption = new Option<bool>("--dry-run", "Show what would be executed without running")
            {
                IsRequired = false
            };
            dryRunOption.AddAlias("-d");

            var outputOption = new Option<DirectoryInfo?>("--out", "Output directory for artifacts and reports")
            {
                IsRequired = false
            };
            outputOption.AddAlias("-o");

            var maxRepairsOption = new Option<int>("--max-repairs", () => 3, "Maximum number of repair attempts")
            {
                IsRequired = false
            };
            maxRepairsOption.AddAlias("-m");

            command.AddOption(requestOption);
            command.AddOption(stdinOption);
            command.AddOption(dryRunOption);
            command.AddOption(outputOption);
            command.AddOption(maxRepairsOption);

            command.SetHandler(async (requestFile, useStdin, dryRun, outputDir, maxRepairs) =>
            {
                var handler = new PipelineRunCommand(serviceProvider, logger);
                await handler.HandleAsync(requestFile, useStdin, dryRun, outputDir, maxRepairs);
            }, requestOption, stdinOption, dryRunOption, outputOption, maxRepairsOption);

            return command;
        }

        /// <summary>
        /// Handles the pipeline run command execution.
        /// </summary>
        public async Task<int> HandleAsync(FileInfo? requestFile, bool useStdin, bool dryRun, DirectoryInfo? outputDir, int maxRepairs)
        {
            try
            {
                // Validate input parameters
                if (requestFile == null && !useStdin)
                {
                    _logger.LogError("Either --request or --stdin must be specified");
                    Console.WriteLine("ERROR: Either --request or --stdin must be specified");
                    return 1;
                }

                if (requestFile != null && useStdin)
                {
                    _logger.LogError("Cannot specify both --request and --stdin");
                    Console.WriteLine("ERROR: Cannot specify both --request and --stdin");
                    return 1;
                }

                // Read request content
                string requestContent;
                if (useStdin)
                {
                    requestContent = await Console.In.ReadToEndAsync();
                    if (string.IsNullOrWhiteSpace(requestContent))
                    {
                        _logger.LogError("No input provided via stdin");
                        Console.WriteLine("ERROR: No input provided via stdin");
                        return 1;
                    }
                }
                else
                {
                    if (!requestFile!.Exists)
                    {
                        _logger.LogError("Request file does not exist: {FilePath}", requestFile.FullName);
                        Console.WriteLine($"ERROR: Request file does not exist: {requestFile.FullName}");
                        return 1;
                    }

                    requestContent = await File.ReadAllTextAsync(requestFile.FullName);
                }

                // Parse request
                var request = await ParseRequestAsync(requestContent);
                if (request == null)
                {
                    _logger.LogError("Failed to parse request content");
                    Console.WriteLine("ERROR: Failed to parse request content");
                    return 1;
                }

                // Set up output directory
                if (outputDir == null)
                {
                    outputDir = new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "nexo-output"));
                }

                if (!outputDir.Exists)
                {
                    outputDir.Create();
                }

                // Execute pipeline
                var result = await ExecutePipelineAsync(request, dryRun, outputDir, maxRepairs);
                
                // Generate reports
                await GenerateReportsAsync(result, outputDir);

                return result.Succeeded ? 0 : 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pipeline run command");
                Console.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        }

        /// <summary>
        /// Parses the request content from JSON or YAML.
        /// </summary>
        private async Task<PipelineConfiguration?> ParseRequestAsync(string content)
        {
            try
            {
                // Try JSON first
                if (content.TrimStart().StartsWith("{"))
                {
                    return JsonSerializer.Deserialize<PipelineConfiguration>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                // Try YAML
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention.Instance)
                    .Build();

                return deserializer.Deserialize<PipelineConfiguration>(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse request content");
                return null;
            }
        }

        /// <summary>
        /// Executes the pipeline with the given request.
        /// </summary>
        private async Task<PipelineExecutionResult> ExecutePipelineAsync(PipelineConfiguration request, bool dryRun, DirectoryInfo outputDir, int maxRepairs)
        {
            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                if (dryRun)
                {
                    return await ExecuteDryRunAsync(request, scope.ServiceProvider);
                }

                // Get the pipeline orchestrator
                var pipelineOrchestrator = scope.ServiceProvider.GetRequiredService<IPipelineOrchestrator>();
                
                // Create a simple application pipeline request for now
                var applicationRequest = new ApplicationPipelineRequest
                {
                    ProjectName = request.Name,
                    Description = request.Description,
                    TargetPlatform = "dotnet",
                    Requirements = request.Commands.Select(c => c.Description).ToList(),
                    OutputDirectory = outputDir.FullName
                };

                // Execute the pipeline
                var result = await pipelineOrchestrator.ExecuteApplicationPipelineAsync(applicationRequest, CancellationToken.None);
                
                return new PipelineExecutionResult
                {
                    Succeeded = result.IsSuccess,
                    ArtifactId = result.ExecutionId,
                    QualityScore = 0.8, // Default quality score
                    Notes = new[] { $"Pipeline executed with status: {result.Status}" },
                    ExecutionTime = result.EndTime - result.StartTime,
                    OutputDirectory = outputDir.FullName
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pipeline");
                return new PipelineExecutionResult
                {
                    Succeeded = false,
                    ArtifactId = Guid.NewGuid().ToString(),
                    QualityScore = 0.0,
                    Notes = new[] { $"Pipeline execution failed: {ex.Message}" },
                    ExecutionTime = TimeSpan.Zero,
                    OutputDirectory = outputDir.FullName,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Executes a dry run of the pipeline.
        /// </summary>
        private async Task<PipelineExecutionResult> ExecuteDryRunAsync(PipelineConfiguration request, IServiceProvider serviceProvider)
        {
            try
            {
                // Validate the pipeline configuration using the orchestrator
                var pipelineOrchestrator = serviceProvider.GetRequiredService<IPipelineOrchestrator>();
                var validationResult = await pipelineOrchestrator.ValidatePipelineConfigurationAsync(request, CancellationToken.None);

                return new PipelineExecutionResult
                {
                    Succeeded = validationResult.IsValid,
                    ArtifactId = Guid.NewGuid().ToString(),
                    QualityScore = validationResult.IsValid ? 1.0 : 0.0,
                    Notes = validationResult.IsValid 
                        ? new[] { "Dry run validation successful" }
                        : validationResult.Issues.Select(i => $"{i.Field}: {i.Message}").ToArray(),
                    ExecutionTime = TimeSpan.Zero,
                    OutputDirectory = "N/A (dry run)",
                    IsDryRun = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during dry run validation");
                return new PipelineExecutionResult
                {
                    Succeeded = false,
                    ArtifactId = Guid.NewGuid().ToString(),
                    QualityScore = 0.0,
                    Notes = new[] { $"Dry run validation failed: {ex.Message}" },
                    ExecutionTime = TimeSpan.Zero,
                    OutputDirectory = "N/A (dry run)",
                    IsDryRun = true,
                    Error = ex.Message
                };
            }
        }

        /// <summary>
        /// Generates human-friendly and machine-readable reports.
        /// </summary>
        internal async Task GenerateReportsAsync(PipelineExecutionResult result, DirectoryInfo outputDir)
        {
            // Generate human-friendly report
            await GenerateHumanReportAsync(result, outputDir);
            
            // Generate machine-readable JSON report
            await GenerateJsonReportAsync(result, outputDir);
        }

        /// <summary>
        /// Generates a human-friendly report.
        /// </summary>
        private async Task GenerateHumanReportAsync(PipelineExecutionResult result, DirectoryInfo outputDir)
        {
            var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.txt");
            
            using var writer = new StreamWriter(reportPath);
            
            await writer.WriteLineAsync("=== NEXO PIPELINE EXECUTION REPORT ===");
            await writer.WriteLineAsync($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            await writer.WriteLineAsync();
            
            await writer.WriteLineAsync("EXECUTION SUMMARY");
            await writer.WriteLineAsync($"Status: {(result.Succeeded ? "SUCCESS" : "FAILED")}");
            await writer.WriteLineAsync($"Artifact ID: {result.ArtifactId}");
            await writer.WriteLineAsync($"Quality Score: {result.QualityScore:F2}");
            await writer.WriteLineAsync($"Execution Time: {result.ExecutionTime}");
            await writer.WriteLineAsync($"Output Directory: {result.OutputDirectory}");
            await writer.WriteLineAsync($"Dry Run: {result.IsDryRun}");
            await writer.WriteLineAsync();
            
            if (!string.IsNullOrEmpty(result.Error))
            {
                await writer.WriteLineAsync("ERRORS");
                await writer.WriteLineAsync(result.Error);
                await writer.WriteLineAsync();
            }
            
            if (result.Notes.Any())
            {
                await writer.WriteLineAsync("NOTES");
                foreach (var note in result.Notes)
                {
                    await writer.WriteLineAsync($"- {note}");
                }
                await writer.WriteLineAsync();
            }
            
            await writer.WriteLineAsync("=== END REPORT ===");
            
            // Also write to console
            Console.WriteLine();
            Console.WriteLine("=== NEXO PIPELINE EXECUTION REPORT ===");
            Console.WriteLine($"Status: {(result.Succeeded ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"Artifact ID: {result.ArtifactId}");
            Console.WriteLine($"Quality Score: {result.QualityScore:F2}");
            Console.WriteLine($"Execution Time: {result.ExecutionTime}");
            Console.WriteLine($"Output Directory: {result.OutputDirectory}");
            Console.WriteLine($"Report saved to: {reportPath}");
            Console.WriteLine("=== END REPORT ===");
        }

        /// <summary>
        /// Generates a machine-readable JSON report.
        /// </summary>
        private async Task GenerateJsonReportAsync(PipelineExecutionResult result, DirectoryInfo outputDir)
        {
            var reportPath = Path.Combine(outputDir.FullName, "pipeline-report.json");
            
            var report = new
            {
                timestamp = DateTime.UtcNow,
                execution = new
                {
                    succeeded = result.Succeeded,
                    artifactId = result.ArtifactId,
                    qualityScore = result.QualityScore,
                    executionTime = result.ExecutionTime.ToString(),
                    outputDirectory = result.OutputDirectory,
                    isDryRun = result.IsDryRun,
                    error = result.Error
                },
                notes = result.Notes,
                metadata = new
                {
                    version = "1.0.0",
                    tool = "nexo-cli"
                }
            };
            
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            await File.WriteAllTextAsync(reportPath, json);
        }
    }

    /// <summary>
    /// Represents the result of a pipeline execution.
    /// </summary>
    public partial class PipelineExecutionResult
    {
        public bool Succeeded { get; set; }
        public string ArtifactId { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public string[] Notes { get; set; } = Array.Empty<string>();
        public TimeSpan ExecutionTime { get; set; }
        public string OutputDirectory { get; set; } = string.Empty;
        public bool IsDryRun { get; set; }
        public string? Error { get; set; }
    }
}
