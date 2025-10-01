using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validation;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Adapters;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// CI/CD tests for error handling and validation.
    /// </summary>
    public class CICDErrorHandlingTests
    {
        private DirectorStudioService _service;
        private TestRunnerIntegration _testRunner;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
            _testRunner = new TestRunnerIntegration(maxRetries: 1, timeoutSeconds: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_ErrorHandling_ShouldBeGraceful()
        {
            // Arrange - Create invalid input
            var invalidBrief = new DesignBrief(
                Description: "", // Empty description
                GenreHint: "InvalidGenre", // Invalid genre
                TargetDurationMinutes: -1, // Invalid duration
                DifficultyLevel: 0, // Invalid difficulty
                Seed: 2000
            );

            // Act
            var result = await CICDTestUtilities.RunMinimalPipelineAsync(invalidBrief);

            // Assert - Should either succeed with defaults or fail gracefully
            if (result.Success)
            {
                Assert.IsNotNull(result.GamePlan, "Should generate a game plan even with invalid input");
                Assert.IsTrue(result.GamePlan.EstimatedDurationMinutes > 0, "Should have valid duration");
            }
            else
            {
                Assert.IsNotNull(result.ErrorMessage, "Should provide error message for failure");
            }

            Debug.Log("✅ CI/CD error handling test passed");
        }

        [Test]
        [Timeout(45000)] // 45 second timeout
        public async Task CICD_ValidationIntegration_ShouldWork()
        {
            // Arrange
            var brief = CICDTestUtilities.CreateTestBrief("CI/CD validation integration test", "FPS", 3000);

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act - Run validation
            var validationResult = await _testRunner.RunValidationTestsAsync(content, plan, CancellationToken.None);

            // Assert
            Assert.IsNotNull(validationResult, "Validation result should not be null");
            Assert.IsTrue(validationResult.TotalTests > 0, "Should have run validation tests");
            Assert.IsTrue(validationResult.OverallScore >= 0, "Validation score should be non-negative");

            Debug.Log($"✅ CI/CD validation integration test passed - Score: {validationResult.OverallScore:F1}%");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_GenreProfiles_ShouldBeAvailable()
        {
            // Arrange
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();

            // Act
            var profiles = genreProfileService.GetAllProfiles();

            // Assert
            Assert.IsNotNull(profiles, "Profiles should not be null");
            Assert.IsTrue(profiles.Count > 0, "Should have at least one genre profile");

            var expectedGenres = new[] { "FPS", "Platformer", "RPG" };
            foreach (var expectedGenre in expectedGenres)
            {
                var profile = profiles.FirstOrDefault(p => p.Name == expectedGenre);
                Assert.IsNotNull(profile, $"Should have profile for {expectedGenre}");
            }

            Debug.Log($"✅ CI/CD genre profiles test passed - {profiles.Count} profiles available");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_AdapterHealth_ShouldRespond()
        {
            // Act - Check adapter health
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyUIAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();

            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyUIHealth = await comfyUIAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);

            // Assert
            Assert.IsNotNull(ollamaHealth, "Ollama health check should return result");
            Assert.IsNotNull(comfyUIHealth, "ComfyUI health check should return result");
            Assert.IsNotNull(piperHealth, "Piper health check should return result");

            // Note: Adapters might not be available in CI, so we just check they respond
            Debug.Log($"Ollama Health: {ollamaHealth.IsHealthy} - {ollamaHealth.Message}");
            Debug.Log($"ComfyUI Health: {comfyUIHealth.IsHealthy} - {comfyUIHealth.Message}");
            Debug.Log($"Piper Health: {piperHealth.IsHealthy} - {piperHealth.Message}");

            Debug.Log("✅ CI/CD adapter health test passed");
        }
    }
}
