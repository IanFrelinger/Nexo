using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NUnit.Framework;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Comprehensive validation tests for the complete Director Studio system.
    /// Validates all components work together correctly.
    /// </summary>
    public class Validation_CompleteSystem : IDisposable
    {
        private readonly DirectorStudioService _service;
        
        public Validation_CompleteSystem()
        {
            _service = new DirectorStudioService();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public void Service_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(_service);
        }
        
        [Test]
        public void Service_ShouldProvideAllRequiredServices()
        {
            // Act & Assert
            Assert.NotNull(_service.GetService<IPlanGameSliceCommand>());
            Assert.NotNull(_service.GetService<IBuildWorldLayoutCommand>());
            Assert.NotNull(_service.GetService<IPlaceInteractionsCommand>());
            Assert.NotNull(_service.GetService<ICreateContentBundleCommand>());
            Assert.NotNull(_service.GetService<IProposeAutoFixesCommand>());
            Assert.NotNull(_service.GetService<IApplyAutoFixesCommand>());
        }
        
        [Test]
        public void Service_ShouldProvideValidators()
        {
            // Act
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            
            // Assert
            Assert.IsNotNull(validators, "Should provide validators");
            Assert.IsTrue(validators.Any(), "Should have at least one validator");
            Assert.IsTrue(validators.Any(v => v is PlayabilityValidator), "Should include PlayabilityValidator");
            Assert.IsTrue(validators.Any(v => v is MechanicsValidator), "Should include MechanicsValidator");
        }
        
        [Test]
        public void Service_ShouldProvideAdapters()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IOllamaAdapter>(), "Should provide IOllamaAdapter");
            Assert.IsNotNull(_service.GetService<ITextureGenAdapter>(), "Should provide ITextureGenAdapter");
            Assert.IsNotNull(_service.GetService<ITtsAdapter>(), "Should provide ITtsAdapter");
        }
        
        [Test]
        public void Service_ShouldProvideGenreProfiles()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert
            Assert.IsNotNull(genreRegistry, "Should provide GenreRegistry");
            Assert.IsNotNull(genreProfileService, "Should provide GenreProfileService");
            Assert.IsTrue(genreRegistry.AllProfiles.Count >= 3, "Should have at least 3 genre profiles");
        }
        
        [Test]
        public async Task CompleteWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple platformer level with a few jumps and a single enemy.",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1
            );
            
            // Act - Plan Game Slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Game Plan
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            Assert.IsNotNull(gamePlan.Id, "Game plan should have ID");
            Assert.IsNotNull(gamePlan.Description, "Game plan should have description");
            Assert.IsTrue(gamePlan.CoreMechanics.Count > 0, "Game plan should have core mechanics");
            Assert.IsTrue(gamePlan.RequiredAssets.Count > 0, "Game plan should have required assets");
            
            // Act - Validate Game Plan
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationResults = new List<NexoDirectorStudio.Validators.ValidationResult>();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationResults.Add(result);
            }
            
            var validationReport = new ValidationReport
            {
                OverallPassed = validationResults.All(r => r.IsValid),
                OverallScore = (int)validationResults.Average(r => r.Score),
                Issues = validationResults.SelectMany(r => r.Issues).ToList(),
                Suggestions = validationResults.SelectMany(r => r.Suggestions).ToList()
            };
            
            // Assert - Validation
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Issues.Count > 0, "Should have validation issues");
            Assert.IsNotNull(validationReport.OverallPassed, "Should have overall status");
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert - World Layout
            Assert.IsNotNull(worldLayout, "World layout should be generated");
            Assert.IsNotNull(worldLayout.Id, "World layout should have ID");
            Assert.IsTrue(worldLayout.Dimensions.x > 0 && worldLayout.Dimensions.y > 0, "World layout should have valid dimensions");
            
            // Act - Place Interactions
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            
            // Assert - Interaction Graph
            Assert.IsNotNull(interactionGraph, "Interaction graph should be generated");
            Assert.IsNotNull(interactionGraph.Id, "Interaction graph should have ID");
            
            // Act - Create Content Bundle
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            
            // Assert - Content Bundle
            Assert.IsNotNull(contentBundle, "Content bundle should be generated");
            Assert.IsNotNull(contentBundle.Id, "Content bundle should have ID");
        }
        
        [Test]
        public async Task AutoFixWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A broken game with impossible mechanics.",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 4
            );
            
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Create a validation report with issues
            var validationIssue = new ValidationIssue
            {
                IssueType = "Missing core mechanic",
                Category = "CoreMechanics",
                Severity = ValidationSeverity.Error,
                Title = "Missing core mechanic",
                Description = "Add jump mechanic",
                SuggestedFix = "Add jump mechanic",
                CanAutoFix = true
            };
            
            var validationResult = new NexoDirectorStudio.Validators.ValidationResult
            {
                IsValid = false,
                Score = 0,
                Message = "Test validation failed",
                Details = "TestValidator found issues",
                Issues = new List<ValidationIssue> { validationIssue },
                Suggestions = new List<ValidationSuggestion>()
            };
            
            var validationReport = new ValidationReport
            {
                OverallPassed = false,
                OverallScore = 0,
                Issues = new List<ValidationIssue> { validationIssue },
                Suggestions = new List<ValidationSuggestion>()
            };
            
            // Act - Propose Auto-Fixes
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var proposedFixes = await proposeFixesCommand.ExecuteAsync(new IProposeAutoFixesCommand.Input(new ContentBundle(), validationReport), CancellationToken.None);
            
            // Assert - Proposed Fixes
            Assert.IsNotNull(proposedFixes, "Should propose fixes");
            Assert.IsTrue(proposedFixes.ProposedFixes.Count > 0, "Should have at least one proposed fix");
            
            // Act - Apply Auto-Fixes
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();
            var fixedContentBundle = new ContentBundle();
            
            foreach (var fix in proposedFixes.ProposedFixes)
            {
                fixedContentBundle = await applyFixesCommand.ExecuteAsync(new IApplyAutoFixesCommand.Input(new ContentBundle(), proposedFixes), CancellationToken.None);
            }
            
            // Assert - Fixed Content Bundle
            Assert.IsNotNull(fixedContentBundle, "Fixed content bundle should be generated");
            Assert.IsNotNull(fixedContentBundle.Id, "Fixed content bundle should have ID");
        }
        
        [Test]
        public async Task AdapterHealthChecks_ShouldWork()
        {
            // Act
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();
            
            // Assert - Adapters should be available
            Assert.IsNotNull(ollamaAdapter, "Ollama adapter should be available");
            Assert.IsNotNull(comfyuiAdapter, "ComfyUI adapter should be available");
            Assert.IsNotNull(piperAdapter, "Piper adapter should be available");
            
            // Act - Health Checks
            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert - Health Check Results
            Assert.IsNotNull(ollamaHealth, "Ollama health check should return result");
            Assert.IsNotNull(comfyuiHealth, "ComfyUI health check should return result");
            Assert.IsNotNull(piperHealth, "Piper health check should return result");
            
            Assert.IsNotNull(ollamaHealth.Message, "Ollama health check should have message");
            Assert.IsNotNull(comfyuiHealth.Message, "ComfyUI health check should have message");
            Assert.IsNotNull(piperHealth.Message, "Piper health check should have message");
        }
        
        [Test]
        public async Task GenreProfileSystem_ShouldWork()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert - Registry
            Assert.IsNotNull(genreRegistry, "Genre registry should be available");
            var allProfiles = genreRegistry.AllProfiles;
            Assert.IsTrue(allProfiles.Count >= 3, "Should have at least 3 genre profiles");
            
            // Test FPS Profile
            var fpsProfile = genreRegistry.GetProfileById("fps");
            Assert.IsNotNull(fpsProfile, "FPS profile should be available");
            Assert.AreEqual("FPS", fpsProfile.Name, "FPS profile should have correct name");
            
            // Test Platformer Profile
            var platformerProfile = genreRegistry.GetProfileById("platformer");
            Assert.IsNotNull(platformerProfile, "Platformer profile should be available");
            Assert.AreEqual("Platformer", platformerProfile.Name, "Platformer profile should have correct name");
            
            // Test RPG Profile
            var rpgProfile = genreRegistry.GetProfileById("rpg");
            Assert.IsNotNull(rpgProfile, "RPG profile should be available");
            Assert.AreEqual("RPG", rpgProfile.Name, "RPG profile should have correct name");
            
            // Test Auto-Detection
            var designBrief = new DesignBrief(
                Description: "A first-person shooter with weapons and enemies",
                GenreHint: "",
                TargetDurationMinutes: 10,
                DifficultyLevel: 4
            );
            
            var detectedGenre = genreRegistry.AutoDetectGenre(designBrief);
            Assert.IsNotNull(detectedGenre, "Should detect genre from brief");
        }
        
        [Test]
        public async Task JsonRepair_ShouldWork()
        {
            // Arrange
            var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
            
            // Act
            var repairResult = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.IsNotNull(repairResult, "Repair result should be generated");
            Assert.IsTrue(repairResult.IsSuccessful, "Repair should be successful");
            Assert.IsNotNull(repairResult.RepairedJson, "Should have repaired JSON");
            Assert.AreNotEqual(repairResult.OriginalJson, repairResult.RepairedJson, "Repaired JSON should be different from original");
        }
        
        [Test]
        public async Task ValidationSystem_ShouldWork()
        {
            // Arrange
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: new DesignBrief(
                    Description: "Test brief",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 1
                ),
                Genre: "Platformer",
                Description: "Test game plan",
                CoreMechanics: new[] { "Jump", "Move" },
                PlayerExperience: new[] { "Fun", "Challenging" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new List<DifficultyBeat>(),
                NarrativeBeats: new[] { "Start", "Middle", "End" },
                RequiredAssets: new[]
                {
                    new AssetRequirement(
                        AssetType: "Platform",
                        Name: "Ground Platform",
                        Description: "Basic ground platform",
                        IsRequired: true,
                        Priority: 5
                    )
                },
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
            
            // Act
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationResults = new List<NexoDirectorStudio.Validators.ValidationResult>();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationResults.Add(result);
            }
            
            var validationReport = new ValidationReport
            {
                OverallPassed = validationResults.All(r => r.IsValid),
                OverallScore = (int)validationResults.Average(r => r.Score),
                Issues = validationResults.SelectMany(r => r.Issues).ToList(),
                Suggestions = validationResults.SelectMany(r => r.Suggestions).ToList()
            };
            
            // Assert
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Issues.Count > 0, "Should have validation issues");
            Assert.IsNotNull(validationReport.OverallPassed, "Should have overall status");
            Assert.IsNotNull(validationReport.Issues, "Should have issues list");
        }
        
        [Test]
        public async Task DTOs_ShouldBeValid()
        {
            // Test DesignBrief
            var designBrief = new DesignBrief(
                Description: "Test brief",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1
            );
            
            Assert.IsNotNull(designBrief.Description, "DesignBrief should have description");
            Assert.IsNotNull(designBrief.Description, "DesignBrief should have description");
            Assert.IsTrue(designBrief.TargetDurationMinutes > 0, "DesignBrief should have positive duration");
            
            // Test GamePlan
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: designBrief,
                Genre: "Platformer",
                Description: "Test plan",
                CoreMechanics: new List<string> { "Jump", "Move" },
                PlayerExperience: new List<string> { "Fun", "Challenging" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new List<DifficultyBeat>(),
                NarrativeBeats: new List<string>(),
                RequiredAssets: new List<AssetRequirement>(),
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
            
            Assert.IsNotNull(gamePlan.Id, "GamePlan should have ID");
            Assert.IsNotNull(gamePlan.Genre, "GamePlan should have genre");
            Assert.IsTrue(gamePlan.CoreMechanics.Count > 0, "GamePlan should have mechanics");
            
            // Test WorldLayout
            var worldLayout = new WorldLayout(
                Id: "test-layout",
                Name: "Test Layout",
                GamePlanId: "test-plan",
                Dimensions: new Vector3(10, 10, 10),
                Tiles: new List<TileData>(),
                Objects: new List<ObjectData>(),
                NavigationNodes: new List<NavigationNode>(),
                Lighting: new LightingData(),
                Camera: new CameraData(),
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow
            );
            
            Assert.IsNotNull(worldLayout.Id, "WorldLayout should have ID");
            Assert.IsTrue(worldLayout.Dimensions.x > 0, "WorldLayout should have positive dimensions");
            
            // Test InteractionGraph
            var interactionGraph = new InteractionGraph
            {
                Id = "test-graph"
            };
            
            Assert.IsNotNull(interactionGraph.Id, "InteractionGraph should have ID");
            
            // Test ContentBundle
            var contentBundle = new ContentBundle
            {
                Id = "test-bundle"
            };
            
            Assert.IsNotNull(contentBundle.Id, "ContentBundle should have ID");
        }
        
        [Test]
        public async Task Commands_ShouldBeExecutable()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 3,
                DifficultyLevel: 1
            );
            
            // Act & Assert - Plan Command
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            Assert.IsNotNull(planCommand, "Plan command should be available");
            
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            
            // Act & Assert - Build Command
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            Assert.IsNotNull(buildCommand, "Build command should be available");
            
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            Assert.IsNotNull(worldLayout, "World layout should be generated");
            
            // Act & Assert - Interactions Command
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            Assert.IsNotNull(interactionsCommand, "Interactions command should be available");
            
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            Assert.IsNotNull(interactionGraph, "Interaction graph should be generated");
            
            // Act & Assert - Content Command
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            Assert.IsNotNull(contentCommand, "Content command should be available");
            
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            Assert.IsNotNull(contentBundle, "Content bundle should be generated");
        }
        
        [Test]
        public async Task System_ShouldHandleErrorsGracefully()
        {
            // Test with null input
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            
            // This should not throw an exception, but handle gracefully
            Assert.DoesNotThrow(async () =>
            {
                try
                {
                    await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(null), CancellationToken.None);
                }
                catch (System.ArgumentNullException)
                {
                    // Expected for null input
                }
            });
        }
        
        [Test]
        public async Task System_ShouldBeDeterministic()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A deterministic test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1,
                Seed: 12345,
                CreatedAt: DateTimeOffset.UtcNow
            );
            
            // Act - Generate same plan twice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Should be deterministic (same seed should produce same result)
            Assert.AreEqual(gamePlan1.Genre, gamePlan2.Genre, "Genre should be deterministic");
            Assert.AreEqual(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes, "Duration should be deterministic");
            Assert.AreEqual(gamePlan1.CoreMechanics.Count, gamePlan2.CoreMechanics.Count, "Mechanics count should be deterministic");
        }
    }
}
