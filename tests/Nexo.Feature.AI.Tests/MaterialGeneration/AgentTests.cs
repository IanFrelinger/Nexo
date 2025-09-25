using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Agents.Specialized;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests.MaterialGeneration
{
    /// <summary>
    /// Tests for Material Generation Agent functionality.
    /// </summary>
    public class AgentTests
    {
        private readonly ILogger _logger;

        public AgentTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public async Task MaterialGenerationAgent_ShouldGenerateMaterial_ForValidRequest()
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
            var response = await agent.ProcessAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Result);
            Assert.True(response.Confidence > 0.5);
        }

        [Fact]
        public async Task MaterialGenerationAgent_ShouldHandleInvalidRequest_Gracefully()
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
            Assert.True(response.Confidence < 0.5);
        }

        [Fact]
        public async Task MaterialGenerationAgent_ShouldOptimizeMaterial_ForTargetPlatform()
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
                    Description = "Mobile-friendly material - optimized for performance",
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
            Assert.True(response.Confidence > 0.7);
        }

        [Fact]
        public async Task MaterialGenerationAgent_ShouldGenerateWeaponMaterial_Successfully()
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
            var response = await agent.ProcessAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.NotNull(response.Result);
            Assert.True(response.Confidence > 0.5);
        }

        [Fact]
        public async Task MaterialGenerationAgent_ShouldGenerateEnemyMaterial_Successfully()
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
                    Description = "Enemy material - organic, dark, menacing",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
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

        [Fact]
        public async Task MaterialGenerationAgent_ShouldHandleConcurrentRequests_Properly()
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
                        Description = "Material 1",
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
                        Description = "Material 2",
                        VisualStyle = "Stylized",
                        TargetPlatform = PlatformType.Mobile,
                        MaterialType = MaterialType.PBR
                    }
                }
            };

            // Act
            var tasks = requests.Select(request => agent.ProcessAsync(request));
            var responses = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(2, responses.Length);
            foreach (var response in responses)
            {
                Assert.True(response.Success);
                Assert.NotNull(response.Result);
            }
        }

        private MaterialGenerationAgent CreateMaterialGenerationAgent()
        {
            return new MaterialGenerationAgent(_logger);
        }
    }
}
