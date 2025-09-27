using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Services;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests.MaterialGeneration
{
    /// <summary>
    /// Tests for Material Generation System integration.
    /// </summary>
    public partial class IntegrationTests
    {
        private readonly ILogger _logger;

        public IntegrationTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldWorkEndToEnd_Successfully()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var analyzer = CreateMaterialContextAnalyzer();
            var generator = CreateDynamicMaterialGenerator();
            var optimizer = CreatePlatformMaterialOptimizer();

            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Weapon material - metallic, dark gray, worn surface",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var agentResponse = await agent.ProcessAsync(request);
            var materialRequest = new MaterialRequest
            {
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var analysisResult = await analyzer.AnalyzeContextAsync(materialRequest);
            var generationResult = await generator.GenerateMaterialAsync(request.Data);
            var optimizationResult = await optimizer.OptimizeForPlatformAsync(generationResult.GeneratedMaterial, PlatformType.Mobile);

            // Assert
            Assert.True(agentResponse.Success);
            Assert.True(analysisResult.Success);
            Assert.True(generationResult.Success);
            Assert.True(optimizationResult.Success);
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldHandleMultipleRequests_Concurrently()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var requests = new List<AgentRequest>
            {
                new AgentRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestType = "GenerateMaterial",
                    Data = new MaterialGenerationRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        Description = "Weapon material - metallic, dark gray, worn surface",
                        VisualStyle = "Realistic",
                        TargetPlatform = PlatformType.Desktop,
                        MaterialType = MaterialType.PBR
                    }
                },
                new AgentRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestType = "GenerateMaterial",
                    Data = new MaterialGenerationRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        Description = "Enemy material - organic, dark, menacing",
                        VisualStyle = "Realistic",
                        TargetPlatform = PlatformType.Desktop,
                        MaterialType = MaterialType.PBR
                    }
                },
                new AgentRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    RequestType = "GenerateMaterial",
                    Data = new MaterialGenerationRequest
                    {
                        RequestId = Guid.NewGuid().ToString(),
                        Description = "Environment material - stone, weathered, ancient",
                        VisualStyle = "Realistic",
                        TargetPlatform = PlatformType.Desktop,
                        MaterialType = MaterialType.PBR
                    }
                }
            };

            // Act
            var tasks = requests.Select(request => agent.ProcessAsync(request));
            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, responses.Length);
            foreach (var response in responses)
            {
                Assert.True(response.Success);
                Assert.NotNull(response.Result);
            }
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldMaintainConsistency_AcrossGenerations()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Weapon material - metallic, dark gray, worn surface",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var response1 = await agent.ProcessAsync(request);
            var response2 = await agent.ProcessAsync(request);

            // Assert
            Assert.True(response1.Success);
            Assert.True(response2.Success);
            Assert.NotNull(response1.Result);
            Assert.NotNull(response2.Result);
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldHandleErrorRecovery_Gracefully()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "", // Invalid empty description
                    VisualStyle = "InvalidStyle",
                    TargetPlatform = PlatformType.Unknown,
                    MaterialType = MaterialType.Unknown
                }
            };

            // Act
            var response = await agent.ProcessAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.NotNull(response.ErrorMessage);
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldHandlePlatformOptimization_EndToEnd()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var optimizer = CreatePlatformMaterialOptimizer();

            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Weapon material - metallic, dark gray, worn surface",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var agentResponse = await agent.ProcessAsync(request);
            var generationResult = await CreateDynamicMaterialGenerator().GenerateMaterialAsync(request.Data);
            var optimizationResult = await optimizer.OptimizeForPlatformAsync(generationResult.GeneratedMaterial, PlatformType.Mobile);

            // Assert
            Assert.True(agentResponse.Success);
            Assert.True(generationResult.Success);
            Assert.True(optimizationResult.Success);
            Assert.NotNull(optimizationResult.OptimizedMaterial);
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldHandleComplexWorkflow_Successfully()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var analyzer = CreateMaterialContextAnalyzer();
            var generator = CreateDynamicMaterialGenerator();
            var optimizer = CreatePlatformMaterialOptimizer();

            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Complex weapon material - metallic, dark gray, worn surface, with rust spots, scratches, and battle damage",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var agentResponse = await agent.ProcessAsync(request);
            var materialRequest = new MaterialRequest
            {
                Description = "Complex weapon material - metallic, dark gray, worn surface, with rust spots, scratches, and battle damage",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var analysisResult = await analyzer.AnalyzeContextAsync(materialRequest);
            var generationResult = await generator.GenerateMaterialAsync(request.Data);
            var optimizationResult = await optimizer.OptimizeForPlatformAsync(generationResult.GeneratedMaterial, PlatformType.Mobile);

            // Assert
            Assert.True(agentResponse.Success);
            Assert.True(analysisResult.Success);
            Assert.True(generationResult.Success);
            Assert.True(optimizationResult.Success);
        }

        [Fact]
        public async Task MaterialGenerationSystem_ShouldHandlePerformanceRequirements_Appropriately()
        {
            // Arrange
            var agent = CreateMaterialGenerationAgent();
            var request = new AgentRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                RequestType = "GenerateMaterial",
                Data = new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "High-performance material - optimized for mobile",
                    VisualStyle = "Stylized",
                    TargetPlatform = PlatformType.Mobile,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var response = await agent.ProcessAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Result);
            Assert.True(response.Confidence > 0.5);
        }

        private MaterialGenerationAgent CreateMaterialGenerationAgent()
        {
            return new MaterialGenerationAgent(_logger);
        }

        private MaterialContextAnalyzer CreateMaterialContextAnalyzer()
        {
            return new MaterialContextAnalyzer(_logger);
        }

        private DynamicMaterialGenerator CreateDynamicMaterialGenerator()
        {
            return new DynamicMaterialGenerator(_logger);
        }

        private PlatformMaterialOptimizer CreatePlatformMaterialOptimizer()
        {
            return new PlatformMaterialOptimizer(_logger);
        }
    }
}
