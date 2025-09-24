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
    public class ModelHealthHandler
    {
        private readonly IModelOrchestrator _modelOrchestrator;
        private readonly IEnumerable<IAIProvider> _providers;
        private readonly ILogger _logger;

        public ModelHealthHandler(IModelOrchestrator modelOrchestrator, IEnumerable<IAIProvider> providers, ILogger logger)
        {
            _modelOrchestrator = modelOrchestrator ?? throw new ArgumentNullException(nameof(modelOrchestrator));
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Command CreateHealthCommand()
        {
            var healthCommand = new Command("health", "Check health status of models and providers");
            
            var modelOption = new Option<string>("--model", "Check specific model health");
            var providerOption = new Option<string>("--provider", "Check specific provider health");
            var detailedOption = new Option<bool>("--detailed", "Show detailed health information");
            
            healthCommand.AddOption(modelOption);
            healthCommand.AddOption(providerOption);
            healthCommand.AddOption(detailedOption);
            
            healthCommand.SetHandler(async (string model, string provider, bool detailed, CancellationToken cancellationToken) =>
            {
                await HandleHealthCheckAsync(model, provider, detailed, cancellationToken);
            }, modelOption, providerOption, detailedOption);

            return healthCommand;
        }

        private async Task HandleHealthCheckAsync(string model, string provider, bool detailed, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Running health check - Model: {Model}, Provider: {Provider}, Detailed: {Detailed}", model, provider, detailed);

                var table = new Table();
                table.AddColumn("Component");
                table.AddColumn("Status");
                table.AddColumn("Details");

                // Check model orchestrator
                await CheckModelOrchestratorHealthAsync(table);

                // Check specific model if provided
                if (!string.IsNullOrEmpty(model))
                {
                    await CheckModelHealthAsync(table, model, detailed);
                }

                // Check specific provider if provided
                if (!string.IsNullOrEmpty(provider))
                {
                    await CheckProviderHealthAsync(table, provider, detailed);
                }

                // Check all providers if no specific provider
                if (string.IsNullOrEmpty(provider))
                {
                    await CheckAllProvidersHealthAsync(table, detailed);
                }

                AnsiConsole.Write(table);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during health check");
                AnsiConsole.WriteException(ex);
            }
        }

        private async Task CheckModelOrchestratorHealthAsync(Table table)
        {
            try
            {
                var isHealthy = await _modelOrchestrator.IsHealthyAsync();
                var status = isHealthy ? "[green]Healthy[/]" : "[red]Unhealthy[/]";
                table.AddRow("Model Orchestrator", status, isHealthy ? "All systems operational" : "Issues detected");
            }
            catch (Exception ex)
            {
                table.AddRow("Model Orchestrator", "[red]Error[/]", ex.Message);
            }
        }

        private async Task CheckModelHealthAsync(Table table, string modelName, bool detailed)
        {
            try
            {
                var isInstalled = await _modelOrchestrator.IsModelInstalledAsync(modelName);
                var status = isInstalled ? "[green]Installed[/]" : "[yellow]Not Installed[/]";
                var details = isInstalled ? "Model is available for use" : "Model needs to be downloaded";
                
                if (detailed && isInstalled)
                {
                    var modelInfo = await _modelOrchestrator.GetModelInfoAsync(modelName);
                    if (modelInfo != null)
                    {
                        details += $" | Size: {FormatSize(modelInfo.SizeInBytes)} | Version: {modelInfo.Version}";
                    }
                }
                
                table.AddRow($"Model: {modelName}", status, details);
            }
            catch (Exception ex)
            {
                table.AddRow($"Model: {modelName}", "[red]Error[/]", ex.Message);
            }
        }

        private async Task CheckProviderHealthAsync(Table table, string providerId, bool detailed)
        {
            try
            {
                var provider = _providers.FirstOrDefault(p => p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));
                if (provider == null)
                {
                    table.AddRow($"Provider: {providerId}", "[red]Not Found[/]", "Provider not registered");
                    return;
                }

                var isAvailable = provider.IsAvailable;
                var status = isAvailable ? "[green]Available[/]" : "[red]Unavailable[/]";
                var details = isAvailable ? "Provider is ready to use" : "Provider is not available";
                
                if (detailed)
                {
                    details += $" | Online: {provider.RequiresOnline} | Display: {provider.DisplayName}";
                }
                
                table.AddRow($"Provider: {providerId}", status, details);
            }
            catch (Exception ex)
            {
                table.AddRow($"Provider: {providerId}", "[red]Error[/]", ex.Message);
            }
        }

        private async Task CheckAllProvidersHealthAsync(Table table, bool detailed)
        {
            foreach (var provider in _providers)
            {
                try
                {
                    var isAvailable = provider.IsAvailable;
                    var status = isAvailable ? "[green]Available[/]" : "[red]Unavailable[/]";
                    var details = isAvailable ? "Provider is ready to use" : "Provider is not available";
                    
                    if (detailed)
                    {
                        details += $" | Online: {provider.RequiresOnline} | Display: {provider.DisplayName}";
                    }
                    
                    table.AddRow($"Provider: {provider.ProviderId}", status, details);
                }
                catch (Exception ex)
                {
                    table.AddRow($"Provider: {provider.ProviderId}", "[red]Error[/]", ex.Message);
                }
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
