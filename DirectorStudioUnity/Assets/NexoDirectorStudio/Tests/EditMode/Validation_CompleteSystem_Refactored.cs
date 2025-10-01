using System;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;
using UnityEngine;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;
using System.Linq;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Comprehensive validation tests for the complete Director Studio system.
    /// Validates all components work together correctly following Nexo patterns.
    /// </summary>
    public class Validation_CompleteSystem_Refactored : IDisposable
    {
        private readonly DirectorStudioService _service;
        
        public Validation_CompleteSystem_Refactored()
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
            Assert.NotNull(validators);
            Assert.True(validators.Any());
            Assert.True(validators.Any(v => v is PlayabilityValidator));
            Assert.True(validators.Any(v => v is MechanicsValidator));
        }
        
        [Test]
        public void Service_ShouldProvideAdapters()
        {
            // Act & Assert
            Assert.NotNull(_service.GetService<IOllamaAdapter>());
            Assert.NotNull(_service.GetService<ITextureGenAdapter>());
            Assert.NotNull(_service.GetService<ITtsAdapter>());
        }
        
        [Test]
        public void Service_ShouldProvideGenreProfiles()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert
            Assert.NotNull(genreRegistry);
            Assert.NotNull(genreProfileService);
                Assert.True(genreRegistry.AllProfiles.Count >= 3);
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
            Assert.NotNull(gamePlan);
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(gamePlan.Description);
            Assert.True(gamePlan.CoreMechanics.Count > 0);
            Assert.True(gamePlan.RequiredAssets.Count > 0);
            
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
            Assert.NotNull(validationReport);
            Assert.True(validationReport.Issues.Count > 0);
            Assert.NotNull(validationReport.OverallPassed);
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert - World Layout
            Assert.NotNull(worldLayout);
            Assert.NotNull(worldLayout.Id);
            Assert.True(worldLayout.Dimensions.x > 0 && worldLayout.Dimensions.y > 0);
            
            // Act - Place Interactions
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            
            // Assert - Interaction Graph
            Assert.NotNull(interactionGraph);
            Assert.NotNull(interactionGraph.Id);
            
            // Act - Create Content Bundle
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            
            // Assert - Content Bundle
            Assert.NotNull(contentBundle);
            Assert.NotNull(contentBundle.Id);
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
            
            var validationResult = new ValidationResult
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
            Assert.NotNull(proposedFixes);
            Assert.True(proposedFixes.ProposedFixes.Count > 0);
            
            // Act - Apply Auto-Fixes
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();
            var fixedContentBundle = new ContentBundle();
            
            foreach (var fix in proposedFixes.ProposedFixes)
            {
                fixedContentBundle = await applyFixesCommand.ExecuteAsync(new IApplyAutoFixesCommand.Input(new ContentBundle(), proposedFixes), CancellationToken.None);
            }
            
            // Assert - Fixed Content Bundle
            Assert.NotNull(fixedContentBundle);
            Assert.NotNull(fixedContentBundle.Id);
        }
        
        [Test]
        public async Task AdapterHealthChecks_ShouldWork()
        {
            // Act
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();
            
            // Assert - Adapters should be available
            Assert.NotNull(ollamaAdapter);
            Assert.NotNull(comfyuiAdapter);
            Assert.NotNull(piperAdapter);
            
            // Act - Health Checks
            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert - Health Check Results
            Assert.NotNull(ollamaHealth);
            Assert.NotNull(comfyuiHealth);
            Assert.NotNull(piperHealth);
            
            Assert.NotNull(ollamaHealth.Message);
            Assert.NotNull(comfyuiHealth.Message);
            Assert.NotNull(piperHealth.Message);
        }
        
        [Test]
        public async Task GenreProfileSystem_ShouldWork()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert - Registry
            Assert.NotNull(genreRegistry);
            var allProfiles = genreRegistry.AllProfiles;
            Assert.True(allProfiles.Count >= 3);
            
            // Test FPS Profile
            var fpsProfile = genreRegistry.GetProfileById("fps");
            Assert.NotNull(fpsProfile);
            Assert.AreEqual("FPS", fpsProfile.Name);
            
            // Test Platformer Profile
            var platformerProfile = genreRegistry.GetProfileById("platformer");
            Assert.NotNull(platformerProfile);
            Assert.AreEqual("Platformer", platformerProfile.Name);
            
            // Test RPG Profile
            var rpgProfile = genreRegistry.GetProfileById("rpg");
            Assert.NotNull(rpgProfile);
            Assert.AreEqual("RPG", rpgProfile.Name);
            
            // Test Auto-Detection
            var designBrief = new DesignBrief(
                Description: "A first-person shooter with weapons and enemies",
                GenreHint: "",
                TargetDurationMinutes: 10,
                DifficultyLevel: 4
            );
            
            var detectedGenre = genreRegistry.AutoDetectGenre(designBrief);
            Assert.NotNull(detectedGenre);
        }
        
        [Test]
        public async Task JsonRepair_ShouldWork()
        {
            // Arrange
            var malformedJson = @"{""name"": ""test"", ""value"": 123,}";
            
            // Act
            var repairResult = JsonRepair.Repair(malformedJson);
            
            // Assert
            Assert.NotNull(repairResult);
            Assert.True(repairResult.IsSuccessful);
            Assert.NotNull(repairResult.RepairedJson);
            Assert.AreNotEqual(repairResult.OriginalJson, repairResult.RepairedJson);
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
            Assert.NotNull(validationReport);
            Assert.True(validationReport.Issues.Count > 0);
            Assert.NotNull(validationReport.OverallPassed);
            Assert.NotNull(validationReport.Issues);
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
            
            Assert.NotNull(designBrief.Description);
            Assert.True(designBrief.TargetDurationMinutes > 0);
            
            // Test GamePlan
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: new DesignBrief(
                    Description: "Test brief",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 1
                ),
                Genre: "Platformer",
                Description: "Test plan",
                CoreMechanics: new[] { "Jump", "Move" },
                PlayerExperience: new[] { "Fun", "Challenging" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new List<DifficultyBeat>(),
                NarrativeBeats: new List<string>(),
                RequiredAssets: new List<AssetRequirement>(),
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
            
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(gamePlan.Genre);
            Assert.True(gamePlan.CoreMechanics.Count > 0);
            
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
            
            Assert.NotNull(worldLayout.Id);
            Assert.True(worldLayout.Dimensions.x > 0);
            
            // Test InteractionGraph
            var interactionGraph = new InteractionGraph
            {
                Id = "test-graph"
            };
            
            Assert.NotNull(interactionGraph.Id);
            
            // Test ContentBundle
            var contentBundle = new ContentBundle
            {
                Id = "test-bundle"
            };
            
            Assert.NotNull(contentBundle.Id);
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
            Assert.NotNull(planCommand);
            
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            Assert.NotNull(gamePlan);
            
            // Act & Assert - Build Command
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            Assert.NotNull(buildCommand);
            
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            Assert.NotNull(worldLayout);
            
            // Act & Assert - Interactions Command
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            Assert.NotNull(interactionsCommand);
            
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            Assert.NotNull(interactionGraph);
            
            // Act & Assert - Content Command
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            Assert.NotNull(contentCommand);
            
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            Assert.NotNull(contentBundle);
        }
        
        [Test]
        public async Task System_ShouldHandleErrorsGracefully()
        {
            // Test with null input
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            
            // This should not throw an exception, but handle gracefully
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(null), CancellationToken.None).Wait();
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
                DifficultyLevel: 1
            );
            
            // Act - Generate same plan twice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Should be deterministic (same seed should produce same result)
            Assert.AreEqual(gamePlan1.Genre, gamePlan2.Genre);
            Assert.AreEqual(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes);
            Assert.AreEqual(gamePlan1.CoreMechanics.Count, gamePlan2.CoreMechanics.Count);
        }
    }
}
