using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Spectre.Console;

namespace Nexo.Infrastructure.Commands.Model
{
    public partial class ModelPullHandler
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger _logger;

        public ModelPullHandler(IModelOrchestrator modelOrchestrator, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreatePullCommand()
        {
            var pullCommand = new Command("pull", "Download and install a model");
            
            var modelArgument = new Argument<string>("model", "Name of the model to download");
            var providerOption = new Option<string>("--provider", "Specify the provider");
            var forceOption = new Option<bool>("--force", "Force download even if model exists");
            
            pullCommand.AddArgument(modelArgument);
            pullCommand.AddOption(providerOption);
            pullCommand.AddOption(forceOption);
            
            pullCommand.SetHandler(async (string model, string provider, bool force, CancellationToken cancellationToken) =>
            {
                await HandlePullModelAsync(model, provider, force, cancellationToken);
            }, modelArgument, providerOption, forceOption);

            return pullCommand;
        }

        private async Task HandlePullModelAsync(string modelName, string provider, bool force, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Pulling model {ModelName} from provider {Provider}", modelName, provider);

                if (!force && await _modelOrchestrator.IsModelInstalledAsync(modelName))
                {
                    AnsiConsole.MarkupLine($"[yellow]Model {modelName} is already installed. Use --force to reinstall.[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[blue]Downloading model {modelName}...[/]");

                var progress = new Progress<double>(value =>
                {
                    AnsiConsole.MarkupLine($"[blue]Progress: {value:P0}[/]");
                });

                var result = await _modelOrchestrator.DownloadModelAsync(modelName, provider, progress, cancellationToken);

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]Successfully downloaded model {modelName}[/]");
                    AnsiConsole.MarkupLine($"[green]Size: {FormatSize(result.SizeInBytes)}[/]");
                    AnsiConsole.MarkupLine($"[green]Location: {result.InstallationPath}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to download model {modelName}: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pulling model {ModelName}", modelName);
                AnsiConsole.WriteException(ex);
            }
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
            return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
        }
    }
}
