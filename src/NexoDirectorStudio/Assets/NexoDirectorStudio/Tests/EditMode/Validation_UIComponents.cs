using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.Editor;
using NexoDirectorStudio.Runtime.DTO;
using NexoDirectorStudio.Runtime.Validators;
using NexoDirectorStudio.Orchestration;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Validation tests for UI components and Director Studio window.
    /// </summary>
    [TestFixture]
    public class Validation_UIComponents
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
            var designBrief = new DesignBrief
            {
                Description = "A simple test game",
                GenreHint = "Platformer",
                TargetDurationMinutes = 5,
                DifficultyLevel = 0.5f
            };
            
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
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test game plan",
                CoreMechanics = new[] { "Jump", "Move" },
                EstimatedDurationMinutes = 5
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
        }
        
        [Test]
        public async Task DirectorStudioWindow_ShouldHandleAutoFixes()
        {
            // Arrange
            var gamePlan = new GamePlan
            {
                Id = "test-plan",
                Genre = "Platformer",
                Description = "Test game plan",
                CoreMechanics = new[] { "Jump" },
                EstimatedDurationMinutes = 5
            };
            
            var validationReport = new ValidationReport();
            validationReport.AddResult(new ValidationResult(
                false,
                "TestValidator",
                "Test validation failed",
                issues: new System.Collections.Generic.List<ValidationIssue>
                {
                    new ValidationIssue("Missing mechanic", ValidationSeverity.Error, "Add move mechanic", "CoreMechanics")
                }
            ));
            
            // Act
            var proposeFixesCommand = _service.GetService<IProposeAutoFixesCommand>();
            var proposedFixes = await proposeFixesCommand.ExecuteAsync(new IProposeAutoFixesCommand.Input(gamePlan, validationReport), CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(proposedFixes, "Should propose fixes");
            Assert.IsTrue(proposedFixes.Count > 0, "Should have at least one proposed fix");
        }
    }
}
