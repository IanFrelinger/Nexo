using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.AI.Services;
using Nexo.Feature.AI.Models;
using Xunit;

namespace Nexo.Feature.AI.Tests.MaterialGeneration
{
    /// <summary>
    /// Tests for Dynamic Material Generator functionality.
    /// </summary>
    public partial class GeneratorTests
    {
        private readonly ILogger _logger;

        public GeneratorTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldGenerateMaterial_ForValidRequest()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.GeneratedMaterial);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldAdaptToRequirements_Correctly()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Mobile-optimized material - simple, efficient",
                VisualStyle = "Stylized",
                TargetPlatform = PlatformType.Mobile,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.GeneratedMaterial);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldHandleComplexRequirements_Properly()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Complex weapon material - metallic, dark gray, worn surface, with rust spots, scratches, and battle damage",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.GeneratedMaterial);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldGenerateWeaponMaterial_Successfully()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.GeneratedMaterial);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldGenerateEnemyMaterial_Successfully()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Enemy material - organic, dark, menacing",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.GeneratedMaterial);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldHandleInvalidRequest_Gracefully()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "", // Invalid empty description
                VisualStyle = "InvalidStyle",
                TargetPlatform = PlatformType.Unknown,
                MaterialType = MaterialType.Unknown
            };

            // Act
            var result = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldHandleConcurrentRequests_Properly()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var requests = new List<MaterialGenerationRequest>
            {
                new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Material 1",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                },
                new MaterialGenerationRequest
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Description = "Material 2",
                    VisualStyle = "Stylized",
                    TargetPlatform = PlatformType.Mobile,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var tasks = requests.Select(request => generator.GenerateMaterialAsync(request));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(2, results.Length);
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.NotNull(result.GeneratedMaterial);
            }
        }

        [Fact]
        public async Task DynamicMaterialGenerator_ShouldMaintainConsistency_AcrossGenerations()
        {
            // Arrange
            var generator = CreateDynamicMaterialGenerator();
            var request = new MaterialGenerationRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                Description = "Consistent material - metallic, dark gray",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result1 = await generator.GenerateMaterialAsync(request);
            var result2 = await generator.GenerateMaterialAsync(request);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.True(result1.Success);
            Assert.True(result2.Success);
            Assert.NotNull(result1.GeneratedMaterial);
            Assert.NotNull(result2.GeneratedMaterial);
        }

        private DynamicMaterialGenerator CreateDynamicMaterialGenerator()
        {
            return new DynamicMaterialGenerator(_logger);
        }
    }
}
