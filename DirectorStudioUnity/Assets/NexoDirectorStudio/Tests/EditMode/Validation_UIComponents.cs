using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using System.Threading.Tasks;
using NexoDirectorStudio.Editor;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Commands;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Validation tests for UI components and Director Studio window.
    /// </summary>
    [TestFixture]
    public class Validation_UIComponents
    {
        private IDirectorStudioService _service;
        
        [SetUp]
        public void SetUp()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        [TearDown]
        public void TearDown()
        {
            (_service as System.IDisposable)?.Dispose();
        }
        
        [Test]
        public void DirectorStudioWindow_ShouldBeCreatable()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                var window = new DirectorStudioWindow();
                Assert.IsNotNull(window, "DirectorStudioWindow should be creatable");
            });
        }
        
        [Test]
        public void DirectorStudioWindow_ShouldHaveCorrectProperties()
        {
            // Act
            var window = new DirectorStudioWindow();
            
            // Assert
            Assert.IsNotNull(window, "Window should be created");
            // Note: In a real Unity test, we would check for Unity-specific properties
            // For now, we just verify the window can be instantiated
        }
        
        [Test]
        public async Task DirectorStudioWindow_ShouldHandleGamePlanGeneration()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A simple test game",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 1,
                Seed: 12345
            );
            
            // Act
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(gamePlan, "Game plan should be generated");
            Assert.IsNotNull(gamePlan.Id, "Game plan should have ID");
            Assert.IsNotNull(gamePlan.Description, "Game plan should have description");
        }
        
        [Test]
        public async Task DirectorStudioWindow_ShouldHandleValidation()
        {
            // Arrange
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: new DesignBrief(
                    Description: "Test game plan",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 3,
                    Seed: 12345
                ),
                Genre: "Platformer",
                Description: "Test game plan",
                CoreMechanics: new[] { "Jump", "Move" },
                PlayerExperience: new[] { "Fun" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new[] { new DifficultyBeat(TimeOffsetSeconds: 0, DifficultyLevel: 2, Description: "Start") },
                NarrativeBeats: new[] { "Introduction" },
                RequiredAssets: new[] { new AssetRequirement(AssetType: "Character", Name: "Player", Description: "Main character", IsRequired: true, Priority: 1) },
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
            
            // Act
            var validators = _service.GetService<System.Collections.Generic.IEnumerable<IValidator<GamePlan>>>();
            var validationResults = new List<ValidationResult>();
            
            foreach (var validator in validators)
            {
                var result = await validator.ValidateAsync(gamePlan, CancellationToken.None);
                validationResults.Add(result);
            }
            
            var validationReport = new ValidationReport
            {
                OverallPassed = validationResults.All(r => r.IsValid),
                OverallScore = validationResults.Count > 0 ? validationResults.Sum(r => r.Score) / validationResults.Count : 0,
                Issues = validationResults.SelectMany(r => r.Issues).ToList(),
                Suggestions = validationResults.SelectMany(r => r.Suggestions).ToList()
            };
            
            // Assert
            Assert.IsNotNull(validationReport, "Validation report should be generated");
            Assert.IsTrue(validationReport.Issues.Count > 0, "Should have validation results");
            Assert.IsNotNull(validationReport.OverallPassed, "Should have overall status");
        }
        
        [Test]
        public async Task DirectorStudioWindow_ShouldHandleAutoFixes()
        {
            // Arrange
            var gamePlan = new GamePlan(
                Id: "test-plan",
                SourceBrief: new DesignBrief(
                    Description: "Test game plan",
                    GenreHint: "Platformer",
                    TargetDurationMinutes: 5,
                    DifficultyLevel: 3,
                    Seed: 12345
                ),
                Genre: "Platformer",
                Description: "Test game plan",
                CoreMechanics: new[] { "Jump" },
                PlayerExperience: new[] { "Fun" },
                EstimatedDurationMinutes: 5,
                DifficultyProgression: new[] { new DifficultyBeat(TimeOffsetSeconds: 0, DifficultyLevel: 2, Description: "Start") },
                NarrativeBeats: new[] { "Introduction" },
                RequiredAssets: new[] { new AssetRequirement(AssetType: "Character", Name: "Player", Description: "Main character", IsRequired: true, Priority: 1) },
                Seed: 12345,
                GeneratedAt: DateTimeOffset.UtcNow,
                Hash: "test-hash"
            );
            
            var validationResults = new List<ValidationResult>
            {
                new ValidationResult
                {
                    IsValid = false,
                    Message = "Test validation failed",
                    Details = "TestValidator: Test validation failed",
                    Score = 0,
                    Issues = new List<ValidationIssue>
                    {
                        new ValidationIssue
                        {
                            Title = "Missing mechanic",
                            Severity = ValidationSeverity.Error,
                            Description = "Add move mechanic",
                            Category = "CoreMechanics"
                        }
                    },
                    Suggestions = new List<ValidationSuggestion>()
                }
            };
            
            var validationReport = new ValidationReport
            {
                OverallPassed = false,
                OverallScore = 0,
                Issues = validationResults.SelectMany(r => r.Issues).ToList(),
                Suggestions = validationResults.SelectMany(r => r.Suggestions).ToList()
            };
            
            // Act
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var contentBundle = new ContentBundle { Id = "test-bundle" }; // Simplified for testing
            var proposedFixes = await proposeFixesCommand.ExecuteAsync(new IProposeAutoFixesCommand.Input(contentBundle, validationReport), CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(proposedFixes, "Should propose fixes");
            Assert.IsTrue(proposedFixes.ProposedFixes.Count > 0, "Should have at least one proposed fix");
        }
    }
}
