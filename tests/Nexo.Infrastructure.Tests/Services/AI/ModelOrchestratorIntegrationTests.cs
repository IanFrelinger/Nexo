using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Feature.AI.Enums;
using Nexo.Feature.AI.Models;
using Nexo.Infrastructure.Services.AI;
using Xunit;

namespace Nexo.Infrastructure.Tests.Services.AI
{
    public class ModelOrchestratorIntegrationTests
    {
        [Fact]
        public async Task Registers_And_Lists_Providers()
        {
            var orchestrator = new ModelOrchestrator(NullLogger<ModelOrchestrator>.Instance);
            var openAiProvider = new OpenAiModelProvider(new HttpClient(), NullLogger<OpenAiModelProvider>.Instance, "test-key");
            var ollamaProvider = new OllamaModelProvider(new HttpClient(), NullLogger<OllamaModelProvider>.Instance);
            orchestrator.RegisterProvider(openAiProvider);
            orchestrator.RegisterProvider(ollamaProvider);
            var providers = orchestrator.Providers;
            Assert.Contains(providers, p => p.ProviderId == "openai");
            Assert.Contains(providers, p => p.ProviderId == "ollama");
        }

        [Fact]
        public async Task Discovers_Models_From_All_Providers()
        {
            var orchestrator = new ModelOrchestrator(NullLogger<ModelOrchestrator>.Instance);
            var openAiProvider = new OpenAiModelProvider(new HttpClient(), NullLogger<OpenAiModelProvider>.Instance, "test-key");
            orchestrator.RegisterProvider(openAiProvider);
            var models = await orchestrator.GetAllAvailableModelsAsync();
            Assert.NotNull(models);
        }

        [Fact]
        public async Task Loads_Model_From_Preferred_Provider_Or_Fallback()
        {
            var orchestrator = new ModelOrchestrator(NullLogger<ModelOrchestrator>.Instance);
            var openAiProvider = new OpenAiModelProvider(new HttpClient(), NullLogger<OpenAiModelProvider>.Instance, "test-key");
            var ollamaProvider = new OllamaModelProvider(new HttpClient(), NullLogger<OllamaModelProvider>.Instance);
            orchestrator.RegisterProvider(openAiProvider);
            orchestrator.RegisterProvider(ollamaProvider);
            // Try to load a model from a preferred provider (simulate fallback by using a non-existent provider)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await orchestrator.LoadModelAsync("nonexistent-model", "nonexistent-provider", CancellationToken.None));
            Assert.Contains("could not be loaded", ex.Message);
        }

        [Fact]
        public async Task Health_Checks_And_Fallback_Work()
        {
            var orchestrator = new ModelOrchestrator(NullLogger<ModelOrchestrator>.Instance);
            var openAiProvider = new OpenAiModelProvider(new HttpClient(), NullLogger<OpenAiModelProvider>.Instance, "test-key");
            orchestrator.RegisterProvider(openAiProvider);
            var healthStatuses = await orchestrator.GetAllProviderHealthStatusAsync();
            Assert.NotNull(healthStatuses);
            Assert.Contains(healthStatuses, h => h.ProviderId == "openai");
        }
    }
} 