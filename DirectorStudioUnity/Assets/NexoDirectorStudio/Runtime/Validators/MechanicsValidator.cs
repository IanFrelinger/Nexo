using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validator for game mechanics.
    /// </summary>
    public class MechanicsValidator : IValidator<GamePlan>
    {
        public async Task<ValidationResult> ValidateAsync(GamePlan item, CancellationToken cancellationToken)
        {
            // Basic implementation - in a real scenario this would validate mechanics
            await Task.Delay(50, cancellationToken); // Simulate async work
            
            return new ValidationResult
            {
                IsValid = true,
                Score = 100,
                Message = "Game mechanics are valid",
                Details = "Mechanics validation passed",
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
                Message = "Invalid item type for mechanics validation",
                Details = "Mechanics validation failed - invalid item type",
                Issues = new List<ValidationIssue>(),
                Suggestions = new List<ValidationSuggestion>()
            };
        }
    }
}