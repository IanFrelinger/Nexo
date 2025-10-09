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
    /// Basic CI/CD tests for core functionality.
    /// </summary>
    public class CICDBasicTests
    {
        private IDirectorStudioService _service;
        private TestRunnerIntegration _testRunner;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
            _testRunner = new TestRunnerIntegration(maxRetries: 1, timeoutSeconds: 5f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_SmokeTest_ShouldPass()
        {
            // Arrange
            var brief = CICDTestUtilities.CreateTestBrief("CI/CD smoke test", "FPS", 123);

            // Act
            var result = await CICDTestUtilities.RunMinimalPipelineAsync(brief);

            // Assert
            Assert.IsTrue(result.Success, $"Smoke test should pass: {result.ErrorMessage}");
            Assert.IsNotNull(result.GamePlan, "Game plan should be generated");
            Assert.IsNotNull(result.WorldLayout, "World layout should be generated");
            Assert.IsNotNull(result.InteractionGraph, "Interaction graph should be generated");
            Assert.IsNotNull(result.ContentBundle, "Content bundle should be generated");

            Debug.Log("✅ CI/CD smoke test passed");
        }

        [Test]
        [Timeout(60000)] // 60 second timeout
        public async Task CICD_AllGenres_ShouldWork()
        {
            // Arrange
            var genres = new[] { "FPS", "Platformer", "RPG" };
            var results = new List<bool>();

            // Act - Test each genre
            foreach (var genre in genres)
            {
                var brief = CICDTestUtilities.CreateTestBrief($"CI/CD test for {genre}", genre, 456);
                var result = await CICDTestUtilities.RunMinimalPipelineAsync(brief);
                results.Add(result.Success);

                Assert.IsTrue(result.Success, $"{genre} pipeline should succeed: {result.ErrorMessage}");
            }

            // Assert
            Assert.AreEqual(3, results.Count, "Should have 3 results");
            Assert.IsTrue(results.All(r => r), "All genres should succeed");

            Debug.Log("✅ CI/CD all genres test passed");
        }

        [Test]
        [Timeout(45000)] // 45 second timeout
        public async Task CICD_ServiceResolution_ShouldWork()
        {
            // Act & Assert - Test that all services can be resolved
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();

            Assert.IsNotNull(planCommand, "Plan command should be resolvable");
            Assert.IsNotNull(buildCommand, "Build command should be resolvable");
            Assert.IsNotNull(interactionsCommand, "Interactions command should be resolvable");
            Assert.IsNotNull(contentCommand, "Content command should be resolvable");
            Assert.IsNotNull(proposeFixesCommand, "Propose fixes command should be resolvable");
            Assert.IsNotNull(applyFixesCommand, "Apply fixes command should be resolvable");

            // Test adapters
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyUIAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();

            Assert.IsNotNull(ollamaAdapter, "Ollama adapter should be resolvable");
            Assert.IsNotNull(comfyUIAdapter, "ComfyUI adapter should be resolvable");
            Assert.IsNotNull(piperAdapter, "Piper adapter should be resolvable");

            // Test validators
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
            Assert.IsNotNull(validators, "Validators should be resolvable");
            Assert.IsTrue(validators.Any(), "Should have at least one validator");

            Debug.Log("✅ CI/CD service resolution test passed");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_DataConsistency_ShouldBeMaintained()
        {
            // Arrange
            var brief = CICDTestUtilities.CreateTestBrief("CI/CD data consistency test", "Platformer", 789);

            // Act
            var result = await CICDTestUtilities.RunMinimalPipelineAsync(brief);

            // Assert
            Assert.IsTrue(result.Success, "Pipeline should succeed");
            Assert.IsTrue(CICDTestUtilities.ValidateDataConsistency(result), "Data consistency should be maintained");

            Debug.Log("✅ CI/CD data consistency test passed");
        }

        [Test]
        [Timeout(30000)] // 30 second timeout
        public async Task CICD_ConcurrentExecution_ShouldNotFail()
        {
            // Arrange
            var briefs = Enumerable.Range(1, 3).Select(i => 
                CICDTestUtilities.CreateTestBrief($"CI/CD concurrent test {i}", 
                    i % 2 == 0 ? "FPS" : "Platformer", 1000 + i)).ToArray();

            // Act - Run pipelines concurrently
            var tasks = briefs.Select(brief => CICDTestUtilities.RunMinimalPipelineAsync(brief)).ToArray();
            var results = await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(3, results.Length, "Should have 3 results");
            Assert.IsTrue(results.All(r => r.Success), "All concurrent pipelines should succeed");

            Debug.Log("✅ CI/CD concurrent execution test passed");
        }
    }
}
