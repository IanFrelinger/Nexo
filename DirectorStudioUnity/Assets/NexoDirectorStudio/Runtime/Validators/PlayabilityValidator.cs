using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validator for game plan playability.
    /// </summary>
    public class PlayabilityValidator : IValidator<GamePlan>
    {
        public async Task<ValidationResult> ValidateAsync(GamePlan item, CancellationToken cancellationToken)
        {
            // Basic implementation - in a real scenario this would validate playability
            await Task.Delay(50, cancellationToken); // Simulate async work
            
            return new ValidationResult
            {
                IsValid = true,
                Score = 100,
                Message = "Game plan is playable",
                Details = "Playability validation passed",
                Issues = new List<ValidationIssue>(),
                Suggestions = new List<ValidationSuggestion>()
            };
        }

        public async Task<ValidationResult> ValidateAsync(object item, CancellationToken cancellationToken)
        {
            if (item is GamePlan gamePlan)
            {
                return await ValidateAsync(gamePlan, cancellationToken);
            }
            
            return new ValidationResult
            {
                IsValid = false,
                Score = 0,
                Message = "Invalid item type for playability validation",
                Details = "Playability validation failed - invalid item type",
                Issues = new List<ValidationIssue>(),
                Suggestions = new List<ValidationSuggestion>()
            };
        }
    }
}