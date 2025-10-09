using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Validators;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Tests for auto-fix workflow validation
    /// </summary>
    public class AutoFixValidationTests : IDisposable
    {
        private readonly IIDirectorStudioService _service;
        
        public AutoFixValidationTests()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
        }
        
        [Test]
        public async Task AutoFixWorkflow_ShouldWork()
        {
            // Arrange
            var designBrief = new DesignBrief(
                Description: "A broken game with impossible mechanics.",
                GenreHint: "Platformer",
                TargetDurationMinutes: 5,
                DifficultyLevel: 0.8f,
                Seed: 456
            );
            
            var planCommand = _service.GetService<IPlanGameSliceCommand>();
            var gamePlan = await planCommand.ExecuteAsync(new IPlanGameSliceCommand.Input(designBrief), CancellationToken.None);
            
            // Create a validation report with issues
            var validationReport = new ValidationReport();
            validationReport.AddResult(new ValidationResult(
                false,
                "TestValidator",
                "Test validation failed",
                issues: new List<ValidationIssue>
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
    }
}
