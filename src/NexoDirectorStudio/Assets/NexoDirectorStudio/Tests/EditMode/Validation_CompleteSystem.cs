using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using NexoDirectorStudio.DTO;
using NexoDirectorStudio.Commands;
using NexoDirectorStudio.Orchestration;
using NexoDirectorStudio.Validators;
using NexoDirectorStudio.Adapters;
using NexoDirectorStudio.Profiles;
using System.Linq;

namespace NexoDirectorStudio.Tests.EditMode
{
    /// <summary>
    /// Comprehensive validation tests for the complete Director Studio system.
    /// Validates all components work together correctly.
    /// 
    /// This class serves as the main orchestrator for validation tests.
    /// Individual test categories are split into separate classes for better organization.
    /// </summary>
    public class Validation_CompleteSystem : IDisposable
    {
        private readonly IIDirectorStudioService _service;
        
        public Validation_CompleteSystem()
        {
            _service = new DirectorStudioServiceUnified();
        }
        
        public void Dispose()
        {
            _service?.Dispose();
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
            var validators = _service.GetService<IEnumerable<IValidator<GamePlan>>>();
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
    }
}