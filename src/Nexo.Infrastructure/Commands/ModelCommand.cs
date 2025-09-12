using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Spectre.Console;
using System.Linq;

namespace Nexo.Infrastructure.Commands
{
    /// <summary>
    /// Model management commands for offline LLama AI integration
    /// </summary>
    public class ModelCommand
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ModelCommand> _logger;
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IEnumerable<IAIProvider> _providers;

        public ModelCommand(IServiceProvider serviceProvider, ILogger<ModelCommand> logger, IModelOrchestrator modelOrchestrator, IEnumerable<IAIProvider> providers)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        /// <summary>
        /// Creates the model command with all subcommands
        /// </summary>
        public Command CreateModelCommand()
        {
            var modelCommand = new Command("model", "Manage AI models for offline LLama integration");

            // List models
            modelCommand.AddCommand(CreateListCommand());

            // Pull/download model
            modelCommand.AddCommand(CreatePullCommand());

            // Remove model
            modelCommand.AddCommand(CreateRemoveCommand());

            // Model info
            modelCommand.AddCommand(CreateInfoCommand());

            // Health check
            modelCommand.AddCommand(CreateHealthCommand());

            return modelCommand;
        }

        /// <summary>
        /// Creates list models command
        /// </summary>
        private Command CreateListCommand()
        {
            var listCommand = new Command("list", "List available and installed models");
            var providerOption = new Option<string>("--provider", "Filter by provider (ollama, native, all)");
            var typeOption = new Option<string>("--type", "Filter by model type (text, code, chat)");
            var availableOption = new Option<bool>("--available", "Show only available models for download");
            var installedOption = new Option<bool>("--installed", "Show only installed models");

            listCommand.AddOption(providerOption);
            listCommand.AddOption(typeOption);
            listCommand.AddOption(availableOption);
            listCommand.AddOption(installedOption);

            listCommand.SetHandler(async (string provider, string type, bool available, bool installed) =>
            {
                try
                {
                    await ListModelsAsync(provider, type, available, installed);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to list models: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to list models");
                }
            }, providerOption, typeOption, availableOption, installedOption);

            return listCommand;
        }

        /// <summary>
        /// Creates pull model command
        /// </summary>
        private Command CreatePullCommand()
        {
            var pullCommand = new Command("pull", "Download a model");
            var modelArgument = new Argument<string>("model", "Model name to download");
            var providerOption = new Option<string>("--provider", "Specify provider (ollama, native)");
            var forceOption = new Option<bool>("--force", "Force download even if model exists");

            pullCommand.AddArgument(modelArgument);
            pullCommand.AddOption(providerOption);
            pullCommand.AddOption(forceOption);

            pullCommand.SetHandler(async (string model, string provider, bool force) =>
            {
                try
                {
                    await PullModelAsync(model, provider, force);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to pull model: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to pull model {ModelName}", model);
                }
            }, modelArgument, providerOption, forceOption);

            return pullCommand;
        }

        /// <summary>
        /// Creates remove model command
        /// </summary>
        private Command CreateRemoveCommand()
        {
            var removeCommand = new Command("remove", "Remove a model");
            var modelArgument = new Argument<string>("model", "Model name to remove");
            var providerOption = new Option<string>("--provider", "Specify provider (ollama, native)");
            var forceOption = new Option<bool>("--force", "Force removal without confirmation");

            removeCommand.AddArgument(modelArgument);
            removeCommand.AddOption(providerOption);
            removeCommand.AddOption(forceOption);

            removeCommand.SetHandler(async (string model, string provider, bool force) =>
            {
                try
                {
                    await RemoveModelAsync(model, provider, force);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to remove model: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to remove model {ModelName}", model);
                }
            }, modelArgument, providerOption, forceOption);

            return removeCommand;
        }

        /// <summary>
        /// Creates model info command
        /// </summary>
        private Command CreateInfoCommand()
        {
            var infoCommand = new Command("info", "Show detailed information about a model");
            var modelArgument = new Argument<string>("model", "Model name to get info for");
            var providerOption = new Option<string>("--provider", "Specify provider (ollama, native)");

            infoCommand.AddArgument(modelArgument);
            infoCommand.AddOption(providerOption);

            infoCommand.SetHandler(async (string model, string provider) =>
            {
                try
                {
                    await ShowModelInfoAsync(model, provider);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to get model info: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to get model info for {ModelName}", model);
                }
            }, modelArgument, providerOption);

            return infoCommand;
        }

        /// <summary>
        /// Creates health check command
        /// </summary>
        private Command CreateHealthCommand()
        {
            var healthCommand = new Command("health", "Check health status of model providers");
            var providerOption = new Option<string>("--provider", "Check specific provider (ollama, native, all)");

            healthCommand.AddOption(providerOption);

            healthCommand.SetHandler(async (string provider) =>
            {
                try
                {
                    await CheckHealthAsync(provider);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]❌ Failed to check health: {ex.Message}[/]");
                    _logger.LogError(ex, "Failed to check health");
                }
            }, providerOption);

            return healthCommand;
        }

        /// <summary>
        /// Lists available and installed models
        /// </summary>
        private async Task ListModelsAsync(string provider, string type, bool available, bool installed)
        {
            AnsiConsole.Write(new FigletText("Model List").Color(Color.Blue));
            AnsiConsole.WriteLine();

            var llamaProviders = _providers.OfType<ILlamaProvider>();

            if (!string.IsNullOrEmpty(provider) && provider != "all")
            {
                llamaProviders = llamaProviders.Where(p => p.Name.Contains(provider, StringComparison.OrdinalIgnoreCase));
            }

            var table = new Table();
            table.AddColumn("[bold]Provider[/]");
            table.AddColumn("[bold]Model Name[/]");
            table.AddColumn("[bold]Type[/]");
            table.AddColumn("[bold]Size[/]");
            table.AddColumn("[bold]Status[/]");
            table.AddColumn("[bold]Capabilities[/]");

            foreach (var llamaProvider in llamaProviders)
            {
                try
                {
                    var models = await llamaProvider.GetAvailableModelsAsync();
                    
                    if (available)
                    {
                        var availableModels = await llamaProvider.GetAvailableModelsForDownloadAsync();
                        models = availableModels.ToList();
                    }

                    foreach (var model in models)
                    {
                        var size = FormatBytes(model.SizeBytes);
                        var status = "[green]Installed[/]";
                        var capabilities = new List<string> { "TextGeneration" };

                        table.AddRow(
                            llamaProvider.Name,
                            model.Name,
                            "TextGeneration",
                            size,
                            status,
                            string.Join(", ", capabilities)
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error listing models for provider {Name}", llamaProvider.Name);
                    table.AddRow(
                        llamaProvider.Name,
                        "[red]Error[/]",
                        "",
                        "",
                        "[red]Error[/]",
                        ""
                    );
                }
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Downloads a model
        /// </summary>
        private async Task PullModelAsync(string modelName, string provider, bool force)
        {
            AnsiConsole.Write(new FigletText("Pull Model").Color(Color.Green));
            AnsiConsole.WriteLine();

            var llamaProviders = _providers.OfType<ILlamaProvider>();

            if (!string.IsNullOrEmpty(provider))
            {
                llamaProviders = llamaProviders.Where(p => p.Name.Contains(provider, StringComparison.OrdinalIgnoreCase));
            }

            ILlamaProvider? selectedProvider = null;
            foreach (var llamaProvider in llamaProviders)
            {
                try
                {
                    var models = await llamaProvider.GetAvailableModelsAsync();
                    if (models.Any(m => m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase)))
                    {
                        selectedProvider = llamaProvider;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking models for provider {Name}", llamaProvider.Name);
                }
            }

            if (selectedProvider == null)
            {
                AnsiConsole.MarkupLine($"[red]❌ No provider found for model: {modelName}[/]");
                return;
            }

            // Check if model already exists
            if (!force)
            {
                var existingModels = await selectedProvider.GetAvailableModelsAsync();
                if (existingModels.Any(m => m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase)))
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠️ Model {modelName} already exists. Use --force to re-download.[/]");
                    return;
                }
            }

            AnsiConsole.MarkupLine($"[blue]📥 Downloading model {modelName} from {selectedProvider.Name}...[/]");

            try
            {
                var modelInfo = await selectedProvider.DownloadModelAsync(modelName);
                
                AnsiConsole.MarkupLine($"[green]✅ Successfully downloaded model: {modelInfo.Name}[/]");
                AnsiConsole.MarkupLine($"[dim]Size: {FormatBytes(modelInfo.SizeBytes)}[/]");
                AnsiConsole.MarkupLine($"[dim]Engine: {modelInfo.EngineType}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed to download model: {ex.Message}[/]");
                throw;
            }
        }

        /// <summary>
        /// Removes a model
        /// </summary>
        private async Task RemoveModelAsync(string modelName, string provider, bool force)
        {
            AnsiConsole.Write(new FigletText("Remove Model").Color(Color.Red));
            AnsiConsole.WriteLine();

            var llamaProviders = _providers.OfType<ILlamaProvider>();

            if (!string.IsNullOrEmpty(provider))
            {
                llamaProviders = llamaProviders.Where(p => p.Name.Contains(provider, StringComparison.OrdinalIgnoreCase));
            }

            ILlamaProvider? selectedProvider = null;
            foreach (var llamaProvider in llamaProviders)
            {
                try
                {
                    var models = await llamaProvider.GetAvailableModelsAsync();
                    if (models.Any(m => m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase)))
                    {
                        selectedProvider = llamaProvider;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking models for provider {Name}", llamaProvider.Name);
                }
            }

            if (selectedProvider == null)
            {
                AnsiConsole.MarkupLine($"[red]❌ Model {modelName} not found in any provider[/]");
                return;
            }

            if (!force)
            {
                var confirmed = AnsiConsole.Confirm($"Are you sure you want to remove model '{modelName}' from {selectedProvider.Name}?");
                if (!confirmed)
                {
                    AnsiConsole.MarkupLine("[yellow]❌ Operation cancelled[/]");
                    return;
                }
            }

            AnsiConsole.MarkupLine($"[blue]🗑️ Removing model {modelName} from {selectedProvider.Name}...[/]");

            try
            {
                var success = await selectedProvider.RemoveModelAsync(modelName);
                
                if (success)
                {
                    AnsiConsole.MarkupLine($"[green]✅ Successfully removed model: {modelName}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[yellow]⚠️ Model {modelName} was not found or could not be removed[/]");
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Failed to remove model: {ex.Message}[/]");
                throw;
            }
        }

        /// <summary>
        /// Shows detailed model information
        /// </summary>
        private async Task ShowModelInfoAsync(string modelName, string provider)
        {
            AnsiConsole.Write(new FigletText("Model Info").Color(Color.Blue));
            AnsiConsole.WriteLine();

            var llamaProviders = _providers.OfType<ILlamaProvider>();

            if (!string.IsNullOrEmpty(provider))
            {
                llamaProviders = llamaProviders.Where(p => p.Name.Contains(provider, StringComparison.OrdinalIgnoreCase));
            }

            Nexo.Core.Domain.Entities.AI.ModelInfo? modelInfo = null;
            ILlamaProvider? selectedProvider = null;

            foreach (var llamaProvider in llamaProviders)
            {
                try
                {
                    var models = await llamaProvider.GetAvailableModelsAsync();
                    var model = models.FirstOrDefault(m => m.Name.Contains(modelName, StringComparison.OrdinalIgnoreCase));
                    
                    if (model != null)
                    {
                        modelInfo = model;
                        selectedProvider = llamaProvider;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking models for provider {Name}", llamaProvider.Name);
                }
            }

            if (modelInfo == null || selectedProvider == null)
            {
                AnsiConsole.MarkupLine($"[red]❌ Model {modelName} not found[/]");
                return;
            }

            // Basic info
            AnsiConsole.MarkupLine($"[bold]Name:[/] {modelInfo.Name}");
            AnsiConsole.MarkupLine($"[bold]Display Name:[/] {modelInfo.Name}");
            AnsiConsole.MarkupLine($"[bold]Provider:[/] {selectedProvider.Name}");
            AnsiConsole.MarkupLine($"[bold]Type:[/] TextGeneration");
            AnsiConsole.MarkupLine($"[bold]Size:[/] {FormatBytes(modelInfo.SizeBytes)}");
            AnsiConsole.MarkupLine($"[bold]Max Context:[/] N/A");
            AnsiConsole.MarkupLine($"[bold]Available:[/] [green]Yes[/]");
            AnsiConsole.MarkupLine($"[bold]Last Updated:[/] N/A");
            AnsiConsole.WriteLine();

            // Capabilities
            AnsiConsole.MarkupLine("[bold]Capabilities:[/]");
            var capabilities = new ModelCapabilities
            {
                SupportsTextGeneration = true,
                SupportsCodeGeneration = true,
                SupportsAnalysis = true,
                SupportsOptimization = false,
                SupportsStreaming = true
            };
            if (capabilities.SupportsTextGeneration) AnsiConsole.MarkupLine("  ✓ Text Generation");
            if (capabilities.SupportsCodeGeneration) AnsiConsole.MarkupLine("  ✓ Code Generation");
            if (capabilities.SupportsAnalysis) AnsiConsole.MarkupLine("  ✓ Analysis");
            if (capabilities.SupportsOptimization) AnsiConsole.MarkupLine("  ✓ Optimization");
            if (capabilities.SupportsStreaming) AnsiConsole.MarkupLine("  ✓ Streaming");
            // Chat support not available in this ModelCapabilities
            AnsiConsole.WriteLine();

            // Provider info
            AnsiConsole.MarkupLine("[bold]Provider Information:[/]");
            AnsiConsole.MarkupLine($"  Priority: {selectedProvider.Priority}");
            AnsiConsole.MarkupLine($"  Offline Capable: {(selectedProvider.IsOfflineCapable ? "[green]Yes[/]" : "[red]No[/]")}");
            AnsiConsole.MarkupLine($"  GPU Acceleration: {(selectedProvider.SupportsGpuAcceleration ? "[green]Yes[/]" : "[red]No[/]")}");
            AnsiConsole.MarkupLine($"  Streaming: {(selectedProvider.SupportsStreaming ? "[green]Yes[/]" : "[red]No[/]")}");
            AnsiConsole.MarkupLine($"  Models Path: {selectedProvider.ModelsPath}");
            AnsiConsole.WriteLine();

            // Memory usage if loaded
            if (selectedProvider.IsModelLoaded(modelName))
            {
                try
                {
                    var memoryUsage = await selectedProvider.GetModelMemoryUsageAsync(modelName);
                    AnsiConsole.MarkupLine($"[bold]Memory Usage:[/] {FormatBytes(memoryUsage)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting memory usage for model {ModelName}", modelName);
                }
            }
        }

        /// <summary>
        /// Checks health status of model providers
        /// </summary>
        private async Task CheckHealthAsync(string provider)
        {
            AnsiConsole.Write(new FigletText("Health Check").Color(Color.Yellow));
            AnsiConsole.WriteLine();

            var llamaProviders = _providers.OfType<ILlamaProvider>();

            if (!string.IsNullOrEmpty(provider) && provider != "all")
            {
                llamaProviders = llamaProviders.Where(p => p.Name.Contains(provider, StringComparison.OrdinalIgnoreCase));
            }

            var table = new Table();
            table.AddColumn("[bold]Provider[/]");
            table.AddColumn("[bold]Status[/]");
            table.AddColumn("[bold]Response Time[/]");
            table.AddColumn("[bold]Error Rate[/]");
            table.AddColumn("[bold]Details[/]");

            foreach (var llamaProvider in llamaProviders)
            {
                try
                {
                    var isHealthy = llamaProvider.IsAvailable();
                    
                    var status = isHealthy ? "[green]Healthy[/]" : "[red]Unhealthy[/]";
                    var responseTime = "N/A";
                    var errorRate = "N/A";
                    var details = isHealthy ? "Available" : "Unavailable";

                    table.AddRow(
                        llamaProvider.Name,
                        status,
                        responseTime,
                        errorRate,
                        details
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking health for provider {Name}", llamaProvider.Name);
                    table.AddRow(
                        llamaProvider.Name,
                        "[red]Error[/]",
                        "N/A",
                        "N/A",
                        ex.Message
                    );
                }
            }

            AnsiConsole.Write(table);
        }

        /// <summary>
        /// Formats bytes into human-readable format
        /// </summary>
        private static string FormatBytes(long bytes)
        {
            if (bytes == 0) return "0 B";
            
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int suffixIndex = 0;
            double size = bytes;
            
            while (size >= 1024 && suffixIndex < suffixes.Length - 1)
            {
                size /= 1024;
                suffixIndex++;
            }
            
            return $"{size:F1} {suffixes[suffixIndex]}";
        }

        /// <summary>
        /// Gets capability names for display
        /// </summary>
        private static List<string> GetCapabilityNames(ModelCapabilities capabilities)
        {
            var names = new List<string>();
            if (capabilities.SupportsTextGeneration) names.Add("Text");
            if (capabilities.SupportsCodeGeneration) names.Add("Code");
            if (capabilities.SupportsAnalysis) names.Add("Analysis");
            if (capabilities.SupportsOptimization) names.Add("Optimization");
            if (capabilities.SupportsStreaming) names.Add("Streaming");
            // Chat support not available in this ModelCapabilities
            return names;
        }
    }
}
