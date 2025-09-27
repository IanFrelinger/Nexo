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
    public partial class ModelRemoveHandler
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly ILogger _logger;

        public ModelRemoveHandler(IModelOrchestrator modelOrchestrator, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateRemoveCommand()
        {
            var removeCommand = new Command("remove", "Remove an installed model");
            
            var modelArgument = new Argument<string>("model", "Name of the model to remove");
            var confirmOption = new Option<bool>("--confirm", "Skip confirmation prompt");
            
            removeCommand.AddArgument(modelArgument);
            removeCommand.AddOption(confirmOption);
            
            removeCommand.SetHandler(async (string model, bool confirm, CancellationToken cancellationToken) =>
            {
                await HandleRemoveModelAsync(model, confirm, cancellationToken);
            }, modelArgument, confirmOption);

            return removeCommand;
        }

        private async Task HandleRemoveModelAsync(string modelName, bool confirm, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Removing model {ModelName}", modelName);

                if (!await _modelOrchestrator.IsModelInstalledAsync(modelName))
                {
                    AnsiConsole.MarkupLine($"[yellow]Model {modelName} is not installed.[/]");
                    return;
                }

                if (!confirm)
                {
                    var shouldRemove = AnsiConsole.Confirm($"Are you sure you want to remove model {modelName}?");
                    if (!shouldRemove)
                    {
                        AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                        return;
                    }
                }

                AnsiConsole.MarkupLine($"[blue]Removing model {modelName}...[/]");

                var result = await _modelOrchestrator.RemoveModelAsync(modelName, cancellationToken);

                if (result.Success)
                {
                    AnsiConsole.MarkupLine($"[green]Successfully removed model {modelName}[/]");
                    AnsiConsole.MarkupLine($"[green]Freed space: {FormatSize(result.FreedSpaceInBytes)}[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]Failed to remove model {modelName}: {result.ErrorMessage}[/]");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing model {ModelName}", modelName);
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
