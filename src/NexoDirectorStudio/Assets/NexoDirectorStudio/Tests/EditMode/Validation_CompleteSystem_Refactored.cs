using Xunit;
using System.Threading;
using System.Threading.Tasks;
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
        
        [Fact]
        public void Service_ShouldBeInitialized()
        {
            // Assert
            Assert.NotNull(_service);
        }
        
        [Fact]
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
        
        [Fact]
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
        
        [Fact]
        public void Service_ShouldProvideAdapters()
        {
            // Act & Assert
            Assert.NotNull(_service.GetService<IOllamaAdapter>());
            Assert.NotNull(_service.GetService<ITextureGenAdapter>());
            Assert.NotNull(_service.GetService<ITtsAdapter>());
        }
        
        [Fact]
        public void Service_ShouldProvideGenreProfiles()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert
            Assert.NotNull(genreRegistry);
            Assert.NotNull(genreProfileService);
            Assert.True(genreRegistry.GetAllProfiles().Count >= 3);
        }
        
        [Fact]
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
            Assert.NotNull(gamePlan);
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(gamePlan.Description);
            Assert.True(gamePlan.CoreMechanics.Length > 0);
            Assert.True(gamePlan.RequiredAssets.Length > 0);
            
            // Act - Validate Game Plan
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationReport = new ValidationReport();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationReport.AddResult(result);
            }
            
            // Assert - Validation
            Assert.NotNull(validationReport);
            Assert.True(validationReport.Results.Count > 0);
            Assert.NotNull(validationReport.OverallStatus);
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert - World Layout
            Assert.NotNull(worldLayout);
            Assert.NotNull(worldLayout.Id);
            Assert.True(worldLayout.GridSize.X > 0 && worldLayout.GridSize.Y > 0);
            
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
        
        [Fact]
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
            Assert.NotNull(proposedFixes);
            Assert.True(proposedFixes.Count > 0);
            
            // Act - Apply Auto-Fixes
            var applyFixesCommand = _service.GetService<IApplyAutoFixesCommand>();
            var fixedGamePlan = gamePlan;
            
            foreach (var fix in proposedFixes)
            {
                fixedGamePlan = await applyFixesCommand.ExecuteAsync(new IApplyAutoFixesCommand.Input(fixedGamePlan, fix.DeltaPlan), CancellationToken.None);
            }
            
            // Assert - Fixed Game Plan
            Assert.NotNull(fixedGamePlan);
            Assert.NotNull(fixedGamePlan.Id);
        }
        
        [Fact]
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
        
        [Fact]
        public async Task GenreProfileSystem_ShouldWork()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            var genreProfileService = _service.GetService<GenreProfileService>();
            
            // Assert - Registry
            Assert.NotNull(genreRegistry);
            var allProfiles = genreRegistry.GetAllProfiles();
            Assert.True(allProfiles.Count >= 3);
            
            // Test FPS Profile
            var fpsProfile = genreRegistry.GetProfile("fps");
            Assert.NotNull(fpsProfile);
            Assert.Equal("FPS", fpsProfile.GenreName);
            
            // Test Platformer Profile
            var platformerProfile = genreRegistry.GetProfile("platformer");
            Assert.NotNull(platformerProfile);
            Assert.Equal("Platformer", platformerProfile.GenreName);
            
            // Test RPG Profile
            var rpgProfile = genreRegistry.GetProfile("rpg");
            Assert.NotNull(rpgProfile);
            Assert.Equal("RPG", rpgProfile.GenreName);
            
            // Test Auto-Detection
            var designBrief = new DesignBrief
            {
                Description = "A first-person shooter with weapons and enemies",
                GenreHint = "",
                TargetDurationMinutes = 10,
                DifficultyLevel = 0.7f
            };
            
            var detectedGenre = genreRegistry.DetectGenre(designBrief);
            Assert.NotNull(detectedGenre);
        }
        
        [Fact]
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
            Assert.NotEqual(repairResult.OriginalJson, repairResult.RepairedJson);
        }
        
        [Fact]
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
            Assert.NotNull(validationReport);
            Assert.True(validationReport.Results.Count > 0);
            Assert.NotNull(validationReport.OverallStatus);
            Assert.NotNull(validationReport.GetSummary());
        }
        
        [Fact]
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
            
            Assert.NotNull(designBrief.Description);
            Assert.True(designBrief.TargetDurationMinutes > 0);
            
            // Test GamePlan
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test plan",
                CoreMechanics = new[] { "Jump", "Move" },
                EstimatedDurationMinutes = 5
            };
            
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(gamePlan.Genre);
            Assert.True(gamePlan.CoreMechanics.Length > 0);
            
            // Test WorldLayout
            var worldLayout = new WorldLayout
            {
                Id = "test-layout",
                Name = "Test Layout",
                GridSize = new Vector2Int(10, 10)
            };
            
            Assert.NotNull(worldLayout.Id);
            Assert.True(worldLayout.GridSize.X > 0);
            
            // Test InteractionGraph
            var interactionGraph = new InteractionGraph
            {
                Id = "test-graph",
                Name = "Test Graph"
            };
            
            Assert.NotNull(interactionGraph.Id);
            
            // Test ContentBundle
            var contentBundle = new ContentBundle
            {
                Id = "test-bundle",
                Name = "Test Bundle"
            };
            
            Assert.NotNull(contentBundle.Id);
        }
        
        [Fact]
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
        
        [Fact]
        public async Task System_ShouldHandleErrorsGracefully()
        {
            // Test with null input
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            
            // This should not throw an exception, but handle gracefully
            await Assert.ThrowsAsync<System.ArgumentNullException>(async () =>
            {
                await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(null), CancellationToken.None);
            });
        }
        
        [Fact]
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
            Assert.Equal(gamePlan1.Genre, gamePlan2.Genre);
            Assert.Equal(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes);
            Assert.Equal(gamePlan1.CoreMechanics.Length, gamePlan2.CoreMechanics.Length);
        }
    }
}
