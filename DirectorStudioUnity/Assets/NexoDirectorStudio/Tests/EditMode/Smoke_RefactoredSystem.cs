using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
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
    /// Smoke tests for the refactored Director Studio system.
    /// Validates that all refactored components work correctly.
    /// </summary>
    public class Smoke_RefactoredSystem : IDisposable
    {
        private readonly DirectorStudioService _service;
        
        public Smoke_RefactoredSystem()
        {
            _service = new DirectorStudioService();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public void Service_ShouldInitialize()
        {
            // Assert
            Assert.NotNull(_service);
        }
        
        [Test]
        public void Commands_ShouldBeAvailable()
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
        public void Validators_ShouldBeAvailable()
        {
            // Act
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            
            // Assert
            Assert.NotNull(validators);
            Assert.True(validators.Any());
        }
        
        [Test]
        public void Adapters_ShouldBeAvailable()
        {
            // Act & Assert
            Assert.NotNull(_service.GetService<IOllamaAdapter>());
            Assert.NotNull(_service.GetService<ITextureGenAdapter>());
            Assert.NotNull(_service.GetService<ITtsAdapter>());
        }
        
        [Test]
        public void GenreProfiles_ShouldBeAvailable()
        {
            // Act
            var genreRegistry = _service.GetService<GenreRegistry>();
            
            // Assert
            Assert.NotNull(genreRegistry);
            Assert.True(genreRegistry.AllProfiles.Count >= 3);
        }
        
        [Test]
        public async Task PlanGameSliceCommand_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            // Act
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert
            Assert.NotNull(gamePlan);
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(gamePlan.Genre);
            Assert.NotNull(gamePlan.Description);
            Assert.True(gamePlan.CoreMechanics.Count > 0);
            Assert.True(gamePlan.RequiredAssets.Count > 0);
        }
        
        [Test]
        public async Task BuildWorldLayoutCommand_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 12345
            );
            
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Act
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Assert
            Assert.NotNull(worldLayout);
            Assert.NotNull(worldLayout.Id);
            Assert.NotNull(worldLayout.Name);
            Assert.NotNull(worldLayout.GamePlanId);
            Assert.True(worldLayout.Tiles.Count > 0);
            Assert.True(worldLayout.Objects.Count > 0);
        }
        
        [Test]
        public async Task ValidationSystem_ShouldWork()
        {
            // Arrange
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: new DesignBrief("Test brief", "Platformer", 5, 3, Seed: 12345),
                Genre: "Platformer",
                Description: "Test game plan",
                CoreMechanics: new[] { "Jump", "Move" },
                PlayerExperience: new[] { "Fun", "Challenging" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new[] { new DifficultyBeat(0, 3, "Start") },
                NarrativeBeats: new[] { "Start", "Middle", "End" },
                RequiredAssets: new[] { new AssetRequirement("Platform", "Ground", "Basic ground platform") },
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
        }
        
        [Test]
        public async Task AdapterHealthChecks_ShouldWork()
        {
            // Act
            var ollamaAdapter = _service.GetService<IOllamaAdapter>();
            var comfyuiAdapter = _service.GetService<ITextureGenAdapter>();
            var piperAdapter = _service.GetService<ITtsAdapter>();
            
            var ollamaHealth = await ollamaAdapter.HealthCheckAsync(CancellationToken.None);
            var comfyuiHealth = await comfyuiAdapter.HealthCheckAsync(CancellationToken.None);
            var piperHealth = await piperAdapter.HealthCheckAsync(CancellationToken.None);
            
            // Assert
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
            var allProfiles = genreRegistry.AllProfiles;
            
            // Assert
            Assert.True(allProfiles.Count >= 3);
            
                var fpsProfile = genreRegistry.GetProfileById("fps");
                var platformerProfile = genreRegistry.GetProfileById("platformer");
                var rpgProfile = genreRegistry.GetProfileById("rpg");
            
            Assert.NotNull(fpsProfile);
            Assert.NotNull(platformerProfile);
            Assert.NotNull(rpgProfile);
            
            Assert.AreEqual("FPS", fpsProfile.Name);
            Assert.AreEqual("Platformer", platformerProfile.Name);
            Assert.AreEqual("RPG", rpgProfile.Name);
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
        public async Task CompleteWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A complete test workflow",
                GenreHint: "Platformer",
                TargetDurationMinutes: 3,
                DifficultyLevel: 2,
                Seed: 54321
            );
            
            // Act - Plan Game Slice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Act - Build World Layout
            var buildCommand = _service.GetService<IBuildWorldLayoutCommand>();
            var worldLayout = await buildCommand.ExecuteAsync(new IBuildWorldLayoutCommand.Input(gamePlan), CancellationToken.None);
            
            // Act - Place Interactions
            var interactionsCommand = _service.GetService<IPlaceInteractionsCommand>();
            var interactionGraph = await interactionsCommand.ExecuteAsync(new IPlaceInteractionsCommand.Input(worldLayout, gamePlan), CancellationToken.None);
            
            // Act - Create Content Bundle
            var contentCommand = _service.GetService<ICreateContentBundleCommand>();
            var contentBundle = await contentCommand.ExecuteAsync(new ICreateContentBundleCommand.Input(interactionGraph, gamePlan), CancellationToken.None);
            
            // Assert - All components should work
            Assert.NotNull(gamePlan);
            Assert.NotNull(worldLayout);
            Assert.NotNull(interactionGraph);
            Assert.NotNull(contentBundle);
            
            Assert.NotNull(gamePlan.Id);
            Assert.NotNull(worldLayout.Id);
            Assert.NotNull(interactionGraph.Id);
            Assert.NotNull(contentBundle.Id);
        }
        
        [Test]
        public async Task System_ShouldBeDeterministic()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A deterministic test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 3,
                Seed: 99999
            );
            
            // Act - Generate same plan twice
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan1 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            var gamePlan2 = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert - Should be deterministic
            Assert.AreEqual(gamePlan1.Genre, gamePlan2.Genre);
            Assert.AreEqual(gamePlan1.EstimatedDurationMinutes, gamePlan2.EstimatedDurationMinutes);
            Assert.AreEqual(gamePlan1.CoreMechanics.Count, gamePlan2.CoreMechanics.Count);
        }
    }
}
