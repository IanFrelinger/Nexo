using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.AI.Models;
using Nexo.Feature.AI.Interfaces;
using Nexo.Feature.AI.Enums;

namespace Nexo.CLI.Tests.Mocks
{
    /// <summary>
    /// Mock AI provider for testing that doesn't require external connections
    /// </summary>
    public class MockAIProvider : IModelProvider
    {
        private readonly string _providerName;
        private readonly bool _shouldFail;

        public MockAIProvider(string providerName = "mock", bool shouldFail = false)
        {
            _providerName = providerName;
            _shouldFail = shouldFail;
        }

        // IModelProvider interface properties
        public string ProviderId => _providerName;
        public string DisplayName => $"Mock {_providerName} Provider";
        public string Name => DisplayName;
        public string ProviderType => "Mock";
        public bool IsEnabled => !_shouldFail;
        public bool IsPrimary => false;

        public IEnumerable<ModelType> SupportedModelTypes => new[]
        {
            ModelType.TextGeneration,
            ModelType.CodeGeneration
        };

        public ModelCapabilities Capabilities => new ModelCapabilities
        {
            SupportsStreaming = true,
            SupportsFunctionCalling = false,
            SupportsTextEmbedding = false,
            MaxInputLength = 4096,
            MaxOutputLength = 4096,
            SupportedLanguages = new List<string> { "en" }
        };

        public async Task<IEnumerable<ModelInfo>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return new[]
            {
                new ModelInfo
                {
                    Id = "mock-model",
                    Name = "mock-model",
                    Description = "Mock model for testing",
                    Version = "1.0",
                    Type = ModelType.TextGeneration,
                    Provider = _providerName,
                    IsAvailable = !_shouldFail,
                    LastUpdated = DateTime.UtcNow
                }
            };
        }

        public async Task<IModel> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return new MockModel(modelName, _providerName, _shouldFail);
        }

        public async Task<ModelResponse> ExecuteAsync(ModelRequest request, CancellationToken cancellationToken = default)
        {
            if (_shouldFail)
            {
                throw new InvalidOperationException("Mock AI provider configured to fail");
            }

            // Simulate some processing time
            await Task.Delay(50, cancellationToken);

            return new ModelResponse
            {
                Content = $"Mock AI response for: {request.Input}",
                Model = "mock-model",
                TotalTokens = request.Input?.Length ?? 0,
                ProcessingTimeMs = 50,
                Metadata = new Dictionary<string, object>
                {
                    ["mock"] = true,
                    ["provider"] = _providerName
                }
            };
        }

        public async Task<ModelValidationResult> ValidateRequestAsync(ModelRequest request)
        {
            await Task.CompletedTask;
            return new ModelValidationResult
            {
                IsValid = !_shouldFail && !string.IsNullOrEmpty(request.Input),
                Errors = _shouldFail ? new List<string> { "Mock provider configured to fail" } : new List<string>()
            };
        }

        public async Task<ModelHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            return new ModelHealthStatus
            {
                ProviderName = _providerName,
                IsHealthy = !_shouldFail,
                LastCheckTime = DateTime.UtcNow,
                ResponseTimeMs = 10,
                ErrorRate = _shouldFail ? 1.0 : 0.0,
                LastError = _shouldFail ? "Mock provider configured to fail" : null
            };
        }

        private class MockModel : IModel
        {
            private readonly string _modelName;
            private readonly string _providerName;
            private readonly bool _shouldFail;

            public MockModel(string modelName, string providerName, bool shouldFail)
            {
                _modelName = modelName;
                _providerName = providerName;
                _shouldFail = shouldFail;
            }

            public ModelInfo Info => new ModelInfo
            {
                Id = _modelName,
                Name = _modelName,
                Description = "Mock model for testing",
                Version = "1.0",
                Type = ModelType.TextGeneration,
                Provider = _providerName,
                IsAvailable = !_shouldFail,
                LastUpdated = DateTime.UtcNow
            };

            public bool IsLoaded => !_shouldFail;

            public async Task<ModelResponse> ProcessAsync(ModelRequest request, CancellationToken cancellationToken = default)
            {
                if (_shouldFail)
                {
                    throw new InvalidOperationException("Mock model configured to fail");
                }

                await Task.Delay(50, cancellationToken);

                return new ModelResponse
                {
                    Content = $"Mock model response for: {request.Input}",
                    Model = _modelName,
                    TotalTokens = request.Input?.Length ?? 0,
                    ProcessingTimeMs = 50,
                    Metadata = new Dictionary<string, object>
                    {
                        ["mock"] = true,
                        ["provider"] = _providerName
                    }
                };
            }

            public IEnumerable<ModelResponseChunk> ProcessStreamAsync(ModelRequest request, CancellationToken cancellationToken = default)
            {
                if (_shouldFail)
                {
                    throw new InvalidOperationException("Mock model configured to fail");
                }

                yield return new ModelResponseChunk
                {
                    Content = $"Mock streaming response for: {request.Input}",
                    IsFinal = true,
                    Index = 0,
                    Metadata = new Dictionary<string, object>
                    {
                        ["mock"] = true,
                        ["provider"] = _providerName,
                        ["model"] = _modelName
                    }
                };
            }

            public ModelCapabilities GetCapabilities()
            {
                return new ModelCapabilities
                {
                    SupportsStreaming = true,
                    SupportsFunctionCalling = false,
                    SupportsTextEmbedding = false,
                    MaxInputLength = 4096,
                    MaxOutputLength = 4096,
                    SupportedLanguages = new List<string> { "en" }
                };
            }

            public async Task UnloadAsync(CancellationToken cancellationToken = default)
            {
                await Task.CompletedTask;
            }
        }
    }
} 