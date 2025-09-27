using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexo.Observability;
using Nexo.Core.Application.Interfaces;
using Nexo.Core.Application.Models;
using Nexo.Core.Application.Services;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Services;
using Nexo.Feature.Platform.Interfaces;
using Nexo.Feature.Platform.Services;
using Nexo.Feature.Analysis.Interfaces;
using Nexo.Feature.Analysis.Services;
using Nexo.Feature.Agent.Interfaces;
using Nexo.Feature.Agent.Services;
using Nexo.Feature.Template.Interfaces;
using Nexo.Feature.Template.Services;
using Nexo.Infrastructure.Services;
using Nexo.Infrastructure.Services.AI;
using Nexo.Infrastructure.Services.Caching;
using Nexo.Shared;
using Nexo.Shared.Models;
using Nexo.Shared.Services;
using Nexo.Shared.Interfaces;
using Nexo.Feature.Pipeline.Interfaces;
using Nexo.Feature.Pipeline.Models;
using Nexo.Feature.Pipeline.Services;
using Nexo.Infrastructure.Services.Resource;
using Nexo.Shared.Interfaces.Resource;
using Nexo.Feature.Factory;
using Nexo.Feature.Unity;
using Nexo.Core.Application.Services.Adaptation;
using Nexo.Core.Extensions;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading;

namespace Nexo.CLI
{
    /// <summary>
    /// AI-specific dependency injection configuration for the Nexo CLI application
    /// </summary>
    public static partial class DependencyInjection
    {
#if !EXCLUDE_AI
        /// <summary>
        /// Configures AI model providers with the orchestrator.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        public static void ConfigureAIProviders(this IServiceProvider serviceProvider)
        {
            var orchestrator = serviceProvider.GetRequiredService<IModelOrchestrator>();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

            // Register OpenAI provider
            var openAiProvider = new OpenAiModelProvider(
                httpClientFactory.CreateClient(),
                loggerFactory.CreateLogger<OpenAiModelProvider>(),
                Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.OpenAiApiKey) ?? Constants.EnvironmentVariables.OpenAiApiKey
            );
            orchestrator.RegisterProviderAsync(openAiProvider, CancellationToken.None).Wait();

            // Register Ollama provider
            var ollamaProvider = new OllamaModelProvider(
                httpClientFactory.CreateClient(),
                loggerFactory.CreateLogger<OllamaModelProvider>()
            );
            orchestrator.RegisterProviderAsync(ollamaProvider, CancellationToken.None).Wait();

            // Register Azure OpenAI provider
            var azureProvider = new AzureOpenAiModelProvider(
                httpClientFactory.CreateClient(),
                loggerFactory.CreateLogger<AzureOpenAiModelProvider>(),
                Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AzureApiKey) ?? Constants.EnvironmentVariables.AzureApiKey,
                Environment.GetEnvironmentVariable(Constants.EnvironmentVariables.AzureEndpoint) ?? "https://your-azure-endpoint.openai.azure.com/"
            );
            orchestrator.RegisterProviderAsync(azureProvider, CancellationToken.None).Wait();
        }
#endif
    }
}
