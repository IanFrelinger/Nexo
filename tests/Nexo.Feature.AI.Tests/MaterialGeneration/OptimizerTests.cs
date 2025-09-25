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
    /// Tests for Platform Material Optimizer functionality.
    /// </summary>
    public class OptimizerTests
    {
        private readonly ILogger _logger;

        public OptimizerTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldOptimizeForPlatform_Correctly()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Mobile;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.Equal(targetPlatform, result.OptimizedMaterial.TargetPlatform);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldBalanceQualityAndPerformance_Appropriately()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Mobile;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.True(result.QualityScore > 0);
            Assert.True(result.PerformanceScore > 0);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldApplyPlatformSpecificOptimizations_Effectively()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Mobile;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.NotNull(result.AppliedOptimizations);
            Assert.True(result.AppliedOptimizations.Count > 0);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleDesktopToMobile_Optimization()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Mobile;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.Equal(targetPlatform, result.OptimizedMaterial.TargetPlatform);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleMobileToDesktop_Optimization()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Mobile-optimized material",
                VisualStyle = "Stylized",
                TargetPlatform = PlatformType.Mobile,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Desktop;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.Equal(targetPlatform, result.OptimizedMaterial.TargetPlatform);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleWebPlatform_Optimization()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Web-optimized material",
                VisualStyle = "Stylized",
                TargetPlatform = PlatformType.Web,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Web;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.OptimizedMaterial);
            Assert.Equal(targetPlatform, result.OptimizedMaterial.TargetPlatform);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleInvalidMaterial_Gracefully()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            GeneratedMaterial material = null;
            var targetPlatform = PlatformType.Mobile;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleUnknownPlatform_Gracefully()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var material = new GeneratedMaterial
            {
                MaterialId = Guid.NewGuid().ToString(),
                Name = "Test Material",
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };
            var targetPlatform = PlatformType.Unknown;

            // Act
            var result = await optimizer.OptimizeForPlatformAsync(material, targetPlatform);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task PlatformMaterialOptimizer_ShouldHandleConcurrentOptimizations_Properly()
        {
            // Arrange
            var optimizer = CreatePlatformMaterialOptimizer();
            var materials = new List<GeneratedMaterial>
            {
                new GeneratedMaterial
                {
                    MaterialId = Guid.NewGuid().ToString(),
                    Name = "Material 1",
                    Description = "Weapon material - metallic, dark gray, worn surface",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                },
                new GeneratedMaterial
                {
                    MaterialId = Guid.NewGuid().ToString(),
                    Name = "Material 2",
                    Description = "Enemy material - organic, dark, menacing",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };
            var targetPlatform = PlatformType.Mobile;

            // Act
            var tasks = materials.Select(material => optimizer.OptimizeForPlatformAsync(material, targetPlatform));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(2, results.Length);
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.NotNull(result.OptimizedMaterial);
            }
        }

        private PlatformMaterialOptimizer CreatePlatformMaterialOptimizer()
        {
            return new PlatformMaterialOptimizer(_logger);
        }
    }
}
