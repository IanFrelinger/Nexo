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
    /// Tests for Material Context Analyzer functionality.
    /// </summary>
    public class AnalyzerTests
    {
        private readonly ILogger _logger;

        public AnalyzerTests(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldAnalyzeContext_Correctly()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldExtractFeatures_FromDescription()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.NotNull(result.Analysis.ExtractedFeatures);
            Assert.True(result.Analysis.ExtractedFeatures.Count > 0);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldIdentifyGenre_FromContext()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Weapon material - metallic, dark gray, worn surface",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.NotNull(result.Analysis.IdentifiedGenre);
            Assert.NotEmpty(result.Analysis.IdentifiedGenre);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldHandleEmptyDescription_Gracefully()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldHandleComplexDescription_Successfully()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Complex weapon material - metallic, dark gray, worn surface, with rust spots, scratches, and battle damage",
                VisualStyle = "Realistic",
                TargetPlatform = PlatformType.Desktop,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.True(result.Confidence > 0.5);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldIdentifyVisualStyle_Correctly()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Stylized weapon material - bright colors, cartoon-like",
                VisualStyle = "Stylized",
                TargetPlatform = PlatformType.Mobile,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.Equal("Stylized", result.Analysis.VisualStyle);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldIdentifyPlatformRequirements_Correctly()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var request = new MaterialRequest
            {
                Description = "Mobile-optimized material",
                VisualStyle = "Stylized",
                TargetPlatform = PlatformType.Mobile,
                MaterialType = MaterialType.PBR
            };

            // Act
            var result = await analyzer.AnalyzeContextAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.NotNull(result.Analysis);
            Assert.Equal(PlatformType.Mobile, result.Analysis.TargetPlatform);
        }

        [Fact]
        public async Task MaterialContextAnalyzer_ShouldHandleMultipleGenres_Successfully()
        {
            // Arrange
            var analyzer = CreateMaterialContextAnalyzer();
            var requests = new List<MaterialRequest>
            {
                new MaterialRequest
                {
                    Description = "Weapon material - metallic, dark gray, worn surface",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                },
                new MaterialRequest
                {
                    Description = "Enemy material - organic, dark, menacing",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                },
                new MaterialRequest
                {
                    Description = "Environment material - stone, weathered, ancient",
                    VisualStyle = "Realistic",
                    TargetPlatform = PlatformType.Desktop,
                    MaterialType = MaterialType.PBR
                }
            };

            // Act
            var tasks = requests.Select(request => analyzer.AnalyzeContextAsync(request));
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.Equal(3, results.Length);
            foreach (var result in results)
            {
                Assert.NotNull(result);
                Assert.True(result.Success);
                Assert.NotNull(result.Analysis);
            }
        }

        private MaterialContextAnalyzer CreateMaterialContextAnalyzer()
        {
            return new MaterialContextAnalyzer(_logger);
        }
    }
}
