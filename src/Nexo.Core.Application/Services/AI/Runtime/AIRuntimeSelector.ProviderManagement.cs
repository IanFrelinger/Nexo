using Microsoft.Extensions.Logging;
using Nexo.Core.Application.Interfaces.AI;
using Nexo.Core.Domain.Entities.AI;
using Nexo.Core.Domain.Enums.AI;
using Nexo.Core.Domain.Entities.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nexo.Core.Application.Services.AI.Runtime
{
    /// <summary>
    /// Provider management functionality for AI runtime selection.
    /// </summary>
    public partial class AIRuntimeSelector
    {
        /// <summary>
        /// Gets all available AI providers
        /// </summary>
        public Task<List<IAIProvider>> GetAvailableProvidersAsync()
        {
            var availableProviders = new List<IAIProvider>();

            foreach (var provider in _providers)
            {
                try
                {
                    if (provider.IsAvailable())
                    {
                        availableProviders.Add(provider);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {ProviderType} failed availability check", provider.ProviderType);
                }
            }

            return Task.FromResult(availableProviders.OrderByDescending(p => p.Priority).ToList());
        }

        /// <summary>
        /// Gets providers that support the specified platform
        /// </summary>
        public async Task<List<IAIProvider>> GetProvidersForPlatformAsync(PlatformType platform)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            return availableProviders
                .Where(p => p.SupportsPlatform(ConvertToEnumsPlatformType(platform)))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        /// <summary>
        /// Gets providers that meet the specified requirements
        /// </summary>
        public async Task<List<IAIProvider>> GetProvidersForRequirementsAsync(AIRequirements requirements)
        {
            var availableProviders = await GetAvailableProvidersAsync();
            return availableProviders
                .Where(p => p.MeetsRequirements(requirements))
                .OrderByDescending(p => p.Priority)
                .ToList();
        }

        /// <summary>
        /// Registers a new AI provider
        /// </summary>
        public void RegisterProvider(IAIProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _logger.LogInformation("Registering AI provider: {ProviderType}", provider.ProviderType);

            _providers.Add(provider);
            _providerMap[provider.ProviderType] = provider;

            // Sort by priority
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        /// <summary>
        /// Unregisters an AI provider
        /// </summary>
        public void UnregisterProvider(AIProviderType providerType)
        {
            _logger.LogInformation("Unregistering AI provider: {ProviderType}", providerType);

            var provider = _providers.FirstOrDefault(p => p.ProviderType == providerType);
            if (provider != null)
            {
                _providers.Remove(provider);
                _providerMap.Remove(providerType);
            }
        }

        private Nexo.Core.Domain.Enums.PlatformType ConvertToEnumsPlatformType(Nexo.Core.Domain.Entities.Infrastructure.PlatformType platformType)
        {
            return platformType switch
            {
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Web => Nexo.Core.Domain.Enums.PlatformType.Web,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Desktop => Nexo.Core.Domain.Enums.PlatformType.Desktop,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Mobile => Nexo.Core.Domain.Enums.PlatformType.Mobile,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Console => Nexo.Core.Domain.Enums.PlatformType.Desktop, // Map Console to Desktop
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Windows => Nexo.Core.Domain.Enums.PlatformType.Windows,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Linux => Nexo.Core.Domain.Enums.PlatformType.Linux,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.macOS => Nexo.Core.Domain.Enums.PlatformType.macOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.WebAssembly => Nexo.Core.Domain.Enums.PlatformType.Web, // Map WebAssembly to Web
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.iOS => Nexo.Core.Domain.Enums.PlatformType.iOS,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Android => Nexo.Core.Domain.Enums.PlatformType.Android,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Cloud => Nexo.Core.Domain.Enums.PlatformType.Cloud,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Docker => Nexo.Core.Domain.Enums.PlatformType.Container,
                Nexo.Core.Domain.Entities.Infrastructure.PlatformType.Other => Nexo.Core.Domain.Enums.PlatformType.CrossPlatform,
                _ => Nexo.Core.Domain.Enums.PlatformType.Unknown
            };
        }
    }
}
