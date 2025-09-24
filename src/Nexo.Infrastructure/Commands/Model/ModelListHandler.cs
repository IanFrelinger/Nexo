using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Spectre.Console;
using System.Linq;

namespace Nexo.Infrastructure.Commands.Model
{
    public class ModelListHandler
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IEnumerable<IAIProvider> _providers;
        private readonly ILogger _logger;

        public ModelListHandler(IModelOrchestrator modelOrchestrator, IEnumerable<IAIProvider> providers, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateListCommand()
        {
            var listCommand = new Command("list", "List available and installed models");
            
            var localOption = new Option<bool>("--local", "Show only locally installed models");
            var remoteOption = new Option<bool>("--remote", "Show only remote available models");
            var providerOption = new Option<string>("--provider", "Filter by provider");
            
            listCommand.AddOption(localOption);
            listCommand.AddOption(remoteOption);
            listCommand.AddOption(providerOption);
            
            listCommand.SetHandler(async (bool local, bool remote, string provider, CancellationToken cancellationToken) =>
            {
                await HandleListModelsAsync(local, remote, provider, cancellationToken);
            }, localOption, remoteOption, providerOption);

            return listCommand;
        }

        private async Task HandleListModelsAsync(bool local, bool remote, string provider, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Listing models - Local: {Local}, Remote: {Remote}, Provider: {Provider}", local, remote, provider);

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Provider");
                table.AddColumn("Status");
                table.AddColumn("Size");
                table.AddColumn("Version");

                if (local || (!local && !remote))
                {
                    await ListLocalModelsAsync(table, provider);
                }

                if (remote || (!local && !remote))
                {
                    await ListRemoteModelsAsync(table, provider);
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing models");
                AnsiConsole.WriteException(ex);
            }
        }

        private async Task ListLocalModelsAsync(Table table, string provider)
        {
            try
            {
                var localModels = await _modelOrchestrator.GetLocalModelsAsync();
                
                foreach (var model in localModels)
                {
                    if (string.IsNullOrEmpty(provider) || model.ProviderId.Equals(provider, StringComparison.OrdinalIgnoreCase))
                    {
                        table.AddRow(
                            model.Name,
                            model.ProviderId,
                            "[green]Installed[/]",
                            FormatSize(model.SizeInBytes),
                            model.Version ?? "Unknown"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing local models");
                table.AddRow("[red]Error[/]", "", "", "", "");
            }
        }

        private async Task ListRemoteModelsAsync(Table table, string provider)
        {
            try
            {
                var availableModels = await _modelOrchestrator.GetAvailableModelsAsync();
                
                foreach (var model in availableModels)
                {
                    if (string.IsNullOrEmpty(provider) || model.ProviderId.Equals(provider, StringComparison.OrdinalIgnoreCase))
                    {
                        var isInstalled = await _modelOrchestrator.IsModelInstalledAsync(model.Name);
                        var status = isInstalled ? "[green]Installed[/]" : "[yellow]Available[/]";
                        
                        table.AddRow(
                            model.Name,
                            model.ProviderId,
                            status,
                            FormatSize(model.SizeInBytes),
                            model.Version ?? "Unknown"
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing remote models");
                table.AddRow("[red]Error[/]", "", "", "", "");
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
