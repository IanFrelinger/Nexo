using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Runtime.Commands;
using NexoDirectorStudio.Runtime.DTO;
using NexoDirectorStudio.Runtime.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;
using System.Linq;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Comprehensive validation tests for the complete Director Studio system.
    /// Validates all components work together correctly.
    /// </summary>
    [TestFixture]
    public class Validation_CompleteSystem
    {
        private DirectorStudioService _service;
        
        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioService();
        }
        
        [TearDown]
        public void TearDown()
        {
            (_service as System.IDisposable)?.Dispose();
        }
        
        [Test]
        public void Service_ShouldBeInitialized()
        {
            // Assert
            Assert.IsNotNull(_service, "DirectorStudioService should be initialized");
        }
        
        [Test]
        public void Service_ShouldProvideAllRequiredServices()
        {
            // Act & Assert
            Assert.IsNotNull(_service.GetService<IPlanGameSliceCommand>(), "Should provide IPlanGameSliceCommand");
            Assert.IsNotNull(_service.GetService<IBuildWorldLayoutCommand>(), "Should provide IBuildWorldLayoutCommand");
            Assert.IsNotNull(_service.GetService<IPlaceInteractionsCommand>(), "Should provide IPlaceInteractionsCommand");
            Assert.IsNotNull(_service.GetService<ICreateContentBundleCommand>(), "Should provide ICreateContentBundleCommand");
            Assert.IsNotNull(_service.GetService<IProposeAutoFixesCommand>(), "Should provide IProposeAutoFixesCommand");
            Assert.IsNotNull(_service.GetService<IApplyAutoFixesCommand>(), "Should provide IApplyAutoFixesCommand");
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
            Assert.IsTrue(genreRegistry.GetAllProfiles().Count >= 3, "Should have at least 3 genre profiles");
        }
        
        [Test]
        public async Task CompleteWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief
            {
                Description = "A simple platformer level with a few jumps and a single enemy.",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 0.5f
            };
            
            // Act - Plan Game Slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Game Plan
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            Assert.IsNotNull(gamePlan.Id, "Game plan should have ID");
            Assert.IsNotNull(gamePlan.Description, "Game plan should have description");
            Assert.IsTrue(gamePlan.CoreMechanics.Length > 0, "Game plan should have core mechanics");
            Assert.IsTrue(gamePlan.RequiredAssets.Length > 0, "Game plan should have required assets");
            
            // Act - Validate Game Plan
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationReport = new ValidationReport();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationReport.AddResult(result);
            }
            
            // Assert - Validation
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Results.Count > 0, "Should have validation results");
            Assert.IsNotNull(validationReport.OverallStatus, "Should have overall status");
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert - World Layout
            Assert.IsNotNull(worldLayout, "World layout should be generated");
            Assert.IsNotNull(worldLayout.Id, "World layout should have ID");
            Assert.IsTrue(worldLayout.GridSize.X > 0 && worldLayout.GridSize.Y > 0, "World layout should have valid grid size");
            
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
            var designBrief = new DesignBrief
            {
                Description = "A broken game with impossible mechanics.",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 0.8f
            };
            
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Create a validation report with issues
            var validationReport = new ValidationReport();
            validationReport.AddResult(new ValidationResult(
                false,
                "TestValidator",
                "Test validation failed",
                issues: new System.Collections.Generic.List<ValidationIssue>
                {
                    new ValidationIssue("Missing core mechanic", ValidationSeverity.Error, "Add jump mechanic", "CoreMechanics")
                }
            ));
            
            // Act - Propose Auto-Fixes
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var proposedFixes = await proposeFixesCommand.ExecuteAsync(new IProposeAutoFixesCommand.Input(gamePlan, validationReport), CancellationToken.None);
            
            // Assert - Proposed Fixes
            Assert.IsNotNull(proposedFixes, "Should propose fixes");
            Assert.IsTrue(proposedFixes.Count > 0, "Should have at least one proposed fix");
            
            // Act - Apply Auto-Fixes
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();
            var fixedGamePlan = gamePlan;
            
            foreach (var fix in proposedFixes)
            {
                fixedGamePlan = await applyFixesCommand.ExecuteAsync(new IApplyAutoFixesCommand.Input(fixedGamePlan, fix.DeltaPlan), CancellationToken.None);
            }
            
            // Assert - Fixed Game Plan
            Assert.IsNotNull(fixedGamePlan, "Fixed game plan should be generated");
            Assert.IsNotNull(fixedGamePlan.Id, "Fixed game plan should have ID");
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
            var allProfiles = genreRegistry.GetAllProfiles();
            Assert.IsTrue(allProfiles.Count >= 3, "Should have at least 3 genre profiles");
            
            // Test FPS Profile
            var fpsProfile = genreRegistry.GetProfile("fps");
            Assert.IsNotNull(fpsProfile, "FPS profile should be available");
            Assert.AreEqual("FPS", fpsProfile.GenreName, "FPS profile should have correct name");
            
            // Test Platformer Profile
            var platformerProfile = genreRegistry.GetProfile("platformer");
            Assert.IsNotNull(platformerProfile, "Platformer profile should be available");
            Assert.AreEqual("Platformer", platformerProfile.GenreName, "Platformer profile should have correct name");
            
            // Test RPG Profile
            var rpgProfile = genreRegistry.GetProfile("rpg");
            Assert.IsNotNull(rpgProfile, "RPG profile should be available");
            Assert.AreEqual("RPG", rpgProfile.GenreName, "RPG profile should have correct name");
            
            // Test Auto-Detection
            var designBrief = new DesignBrief
            {
                Description = "A first-person shooter with weapons and enemies",
                GenreHint = "",
                TargetDurationMinutes = 10,
                DifficultyLevel = 0.7f
            };
            
            var detectedGenre = genreRegistry.DetectGenre(designBrief);
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
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test game plan",
                CoreMechanics = new[] { "Jump", "Move" },
                PlayerExperience = new[] { "Fun", "Challenging" },
                EstimatedDurationMinutes = 5,
                NarrativeBeats = new[] { "Start", "Middle", "End" },
                RequiredAssets = new[]
                {
                    new AssetRequirement
                    {
                        AssetType = "Platform",
                        Name = "Ground Platform",
                        Description = "Basic ground platform",
                        IsRequired = true,
                        Priority = 5
                    }
                }
            };
            
            // Act
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationReport = new ValidationReport();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationReport.AddResult(result);
            }
            
            // Assert
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Results.Count > 0, "Should have validation results");
            Assert.IsNotNull(validationReport.OverallStatus, "Should have overall status");
            Assert.IsNotNull(validationReport.GetSummary(), "Should have summary");
        }
        
        [Test]
        public async Task DTOs_ShouldBeValid()
        {
            // Test DesignBrief
            var designBrief = new DesignBrief
            {
                Description = "Test brief",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 0.5f
            };
            
            Assert.IsNotNull(designBrief.Id, "DesignBrief should have ID");
            Assert.IsNotNull(designBrief.Description, "DesignBrief should have description");
            Assert.IsTrue(designBrief.TargetDurationMinutes > 0, "DesignBrief should have positive duration");
            
            // Test GamePlan
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test plan",
                CoreMechanics = new[] { "Jump", "Move" },
                EstimatedDurationMinutes = 5
            };
            
            Assert.IsNotNull(gamePlan.Id, "GamePlan should have ID");
            Assert.IsNotNull(gamePlan.Genre, "GamePlan should have genre");
            Assert.IsTrue(gamePlan.CoreMechanics.Length > 0, "GamePlan should have mechanics");
            
            // Test WorldLayout
            var worldLayout = new WorldLayout
            {
                Id = "test-layout",
                Name = "Test Layout",
                GridSize = new Vector2Int(10, 10)
            };
            
            Assert.IsNotNull(worldLayout.Id, "WorldLayout should have ID");
            Assert.IsTrue(worldLayout.GridSize.X > 0, "WorldLayout should have positive grid size");
            
            // Test InteractionGraph
            var interactionGraph = new InteractionGraph
            {
                Id = "test-graph",
                Name = "Test Graph"
            };
            
            Assert.IsNotNull(interactionGraph.Id, "InteractionGraph should have ID");
            
            // Test ContentBundle
            var contentBundle = new ContentBundle
            {
                Id = "test-bundle",
                Name = "Test Bundle"
            };
            
            Assert.IsNotNull(contentBundle.Id, "ContentBundle should have ID");
        }
        
        [Test]
        public async Task Commands_ShouldBeExecutable()
        {
            // Arrange
            var designBrief = new DesignBrief
            {
                Description = "A simple test game",
                GenreHint = "Platformer",
                TargetDurationMinutes = 3,
                DifficultyLevel = 0.3f
            };
            
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
            Assert.DoesNotThrowAsync(async () =>
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
            var designBrief = new DesignBrief
            {
                Description = "A deterministic test game",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 0.5f
            };
            
            // Act - Generate same plan twice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Should be deterministic (same seed should produce same result)
            Assert.AreEqual(gamePlan1.Genre, gamePlan2.Genre, "Genre should be deterministic");
            Assert.AreEqual(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes, "Duration should be deterministic");
            Assert.AreEqual(gamePlan1.CoreMechanics.Length, gamePlan2.CoreMechanics.Length, "Mechanics count should be deterministic");
        }
    }
}
