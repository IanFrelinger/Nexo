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
    public class ModelInfoHandler
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger _logger;

        public ModelInfoHandler(IModelOrchestrator modelOrchestrator, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateInfoCommand()
        {
            var infoCommand = new Command("info", "Show detailed information about a model");
            
            var modelArgument = new Argument<string>("model", "Name of the model to get information about");
            
            infoCommand.AddArgument(modelArgument);
            
            infoCommand.SetHandler(async (string model, CancellationToken cancellationToken) =>
            {
                await HandleModelInfoAsync(model, cancellationToken);
            }, modelArgument);

            return infoCommand;
        }

        private async Task HandleModelInfoAsync(string modelName, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting information for model {ModelName}", modelName);

                var modelInfo = await _modelOrchestrator.GetModelInfoAsync(modelName);

                if (modelInfo == null)
                {
                    AnsiConsole.MarkupLine($"[red]Model {modelName} not found.[/]");
                    return;
                }

                // Create info panel
                var panel = new Panel(FormatModelInfo(modelInfo))
                {
                    Header = new PanelHeader($"Model: {modelName}", Justify.Left),
                    Border = BoxBorder.Rounded
                };

                AnsiConsole.Write(panel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model info for {ModelName}", modelName);
                AnsiConsole.WriteException(ex);
            }
        }

        private string FormatModelInfo(dynamic modelInfo)
        {
            var info = $@"[bold]Name:[/] {modelInfo.Name}
[bold]Provider:[/] {modelInfo.ProviderId}
[bold]Version:[/] {modelInfo.Version ?? "Unknown"}
[bold]Size:[/] {FormatSize(modelInfo.SizeInBytes)}
[bold]Status:[/] {modelInfo.IsInstalled ? "[green]Installed[/]" : "[yellow]Available[/]"}
[bold]Description:[/] {modelInfo.Description ?? "No description available"}
[bold]Tags:[/] {string.Join(", ", modelInfo.Tags ?? new string[0])}
[bold]Last Updated:[/] {modelInfo.LastUpdated:yyyy-MM-dd HH:mm:ss}
[bold]Installation Path:[/] {modelInfo.InstallationPath ?? "Not installed"}";

            if (modelInfo.Capabilities != null)
            {
                info += $"\n[bold]Capabilities:[/]\n";
                foreach (var capability in modelInfo.Capabilities)
                {
                    info += $"  • {capability}\n";
                }
            }

            if (modelInfo.Requirements != null)
            {
                info += $"\n[bold]Requirements:[/]\n";
                foreach (var requirement in modelInfo.Requirements)
                {
                    info += $"  • {requirement}\n";
                }
            }

            return info;
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
