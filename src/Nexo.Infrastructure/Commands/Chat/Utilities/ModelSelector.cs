using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Feature.AI.Interfaces;
using Nexo.Infrastructure.Services.AI;

namespace Nexo.Infrastructure.Commands.Chat.Utilities
{
    /// <summary>
    /// Handles model selection logic
    /// </summary>
    public partial class ModelSelector
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ChatCommand> _logger;

        public ModelSelector(IServiceProvider serviceProvider, ILogger<ChatCommand> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Selects the best model for the request
        /// </summary>
        public async Task<IModel?> SelectModelAsync(string modelPreference, bool preferCodeModels = false)
        {
            try
            {
                var providers = _serviceProvider.GetServices<ILlamaProvider>();
                var llamaProviders = providers.OfType<ILlamaProvider>().OrderByDescending(p => p.Priority);

                if (modelPreference == "auto")
                {
                    // Select the highest priority available provider
                    foreach (var provider in llamaProviders)
                    {
                        if (provider.IsOfflineCapable)
                        {
                            var models = await provider.GetAvailableModelsAsync();
                            var selectedModel = models.FirstOrDefault();

                            if (selectedModel != null)
                            {
                                await provider.LoadModelAsync(selectedModel.Name);
                                return new LlamaNativeModel(selectedModel.Name, _logger, (LlamaNativeProvider)provider);
                            }
                        }
                    }
                }
                else
                {
                    // Try to find specific model
                    foreach (var provider in llamaProviders)
                    {
                        var models = await provider.GetAvailableModelsAsync();
                        var model = models.FirstOrDefault(m => m.Name.Contains(modelPreference, StringComparison.OrdinalIgnoreCase));
                        
                        if (model != null)
                        {
                            await provider.LoadModelAsync(model.Name);
                            return new LlamaNativeModel(model.Name, _logger, (LlamaNativeProvider)provider);
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting model");
                return null;
            }
        }
    }
}
