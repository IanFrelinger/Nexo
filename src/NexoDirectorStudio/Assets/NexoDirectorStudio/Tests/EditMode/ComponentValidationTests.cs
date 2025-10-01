using System.Collections;
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

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Unity test runner integration for component validation.
    /// These tests can be run from the Unity Test Runner window.
    /// </summary>
    public class ComponentValidationTests
    {
        private DirectorStudioService _service;
        private ComponentValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
            _validator = new ComponentValidator(maxRetries: 3, timeoutSeconds: 30f);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
        }

        [Test]
        public async Task ValidateFPSComponents_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Doom-style FPS with weapons and enemies",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 123
            );

            // Act
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Assets.Count > 0, "Content bundle should contain assets");

            // Validate components
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);
            
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            Assert.IsTrue(validationResult.OverallScore >= 80f, 
                $"Component validation score too low: {validationResult.OverallScore}%");
        }

        [Test]
        public async Task ValidatePlatformerComponents_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Mario-style platformer with jumps and collectibles",
                GenreHint: "Platformer",
                TargetDurationMinutes: 3,
                DifficultyLevel: 2,
                Seed: 456
            );

            // Act
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Assets.Count > 0, "Content bundle should contain assets");

            // Validate components
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);
            
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            Assert.IsTrue(validationResult.OverallScore >= 80f, 
                $"Component validation score too low: {validationResult.OverallScore}%");
        }

        [Test]
        public async Task ValidateRPGComponents_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Fantasy RPG with NPCs and quests",
                GenreHint: "RPG",
                TargetDurationMinutes: 10,
                DifficultyLevel: 4,
                Seed: 789
            );

            // Act
            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Assert
            Assert.IsNotNull(content);
            Assert.IsTrue(content.Assets.Count > 0, "Content bundle should contain assets");

            // Validate components
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);
            
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            Assert.IsTrue(validationResult.OverallScore >= 80f, 
                $"Component validation score too low: {validationResult.OverallScore}%");
        }

        [Test]
        public async Task ValidateComponentsWithRetry_ShouldEventuallyPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Complex FPS with multiple weapon types and enemy AI",
                GenreHint: "FPS",
                TargetDurationMinutes: 8,
                DifficultyLevel: 5,
                Seed: 999
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act - Validate with retry logic
            var validationResult = await _validator.ValidateWithRetryAsync(
                content, 
                plan, 
                async (failedResult) =>
                {
                    // Regenerate content based on validation failures
                    Debug.Log($"Regenerating content due to validation failures: {failedResult.ValidationError}");
                    
                    // In a real implementation, this would use the validation results
                    // to guide the regeneration process
                    return await _service.GetService<ICreateContentBundleCommand>()
                        .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);
                },
                CancellationToken.None);

            // Assert
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed after retries: {validationResult.ValidationError}");
            Assert.IsTrue(validationResult.OverallScore >= 70f, 
                $"Component validation score too low after retries: {validationResult.OverallScore}%");
        }

        [Test]
        public async Task ValidatePerformanceRequirements_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Performance-optimized FPS for mobile devices",
                GenreHint: "FPS",
                TargetDurationMinutes: 3,
                DifficultyLevel: 2,
                Seed: 111
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);

            // Assert
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            
            // Check performance-specific test results
            var performanceSuite = validationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "PerformanceTestSuite");
            
            Assert.IsNotNull(performanceSuite, "Performance test suite should be present");
            Assert.IsTrue(performanceSuite.Passed, "Performance tests should pass");
        }

        [Test]
        public async Task ValidateAccessibilityRequirements_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Accessible platformer with high contrast and large text",
                GenreHint: "Platformer",
                TargetDurationMinutes: 4,
                DifficultyLevel: 1,
                Seed: 222
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);

            // Assert
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            
            // Check accessibility-specific test results
            var accessibilitySuite = validationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "AccessibilityTestSuite");
            
            Assert.IsNotNull(accessibilitySuite, "Accessibility test suite should be present");
            Assert.IsTrue(accessibilitySuite.Passed, "Accessibility tests should pass");
        }

        [Test]
        public async Task ValidateIntegrationRequirements_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Integrated RPG with complex interactions between NPCs and quests",
                GenreHint: "RPG",
                TargetDurationMinutes: 6,
                DifficultyLevel: 3,
                Seed: 333
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);

            // Assert
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            
            // Check integration-specific test results
            var integrationSuite = validationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "IntegrationTestSuite");
            
            Assert.IsNotNull(integrationSuite, "Integration test suite should be present");
            Assert.IsTrue(integrationSuite.Passed, "Integration tests should pass");
        }

        [Test]
        public async Task ValidateGenreSpecificRequirements_ShouldPass()
        {
            // Arrange
            var brief = new DesignBrief(
                Description: "Genre-specific FPS with proper weapon mechanics and enemy AI",
                GenreHint: "FPS",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4,
                Seed: 444
            );

            var plan = await _service.GetService<IPlanGameSliceCommand>()
                .ExecuteAsync(new IPlanGameSliceCommand.Input(brief), CancellationToken.None);

            var world = await _service.GetService<IBuildWorldLayoutCommand>()
                .ExecuteAsync(new IBuildWorldLayoutCommand.Input(plan), CancellationToken.None);

            var interactions = await _service.GetService<IPlaceInteractionsCommand>()
                .ExecuteAsync(new IPlaceInteractionsCommand.Input(world, plan), CancellationToken.None);

            var content = await _service.GetService<ICreateContentBundleCommand>()
                .ExecuteAsync(new ICreateContentBundleCommand.Input(interactions, plan), CancellationToken.None);

            // Act
            var validationResult = await _validator.ValidateAsync(content, plan, CancellationToken.None);

            // Assert
            Assert.IsTrue(validationResult.OverallPassed, 
                $"Component validation failed: {validationResult.ValidationError}");
            
            // Check genre-specific test results
            var genreSuite = validationResult.TestSuiteResults
                .FirstOrDefault(r => r.TestSuiteName == "GenreSpecificTestSuite");
            
            Assert.IsNotNull(genreSuite, "Genre-specific test suite should be present");
            Assert.IsTrue(genreSuite.Passed, "Genre-specific tests should pass");
        }
    }
}
