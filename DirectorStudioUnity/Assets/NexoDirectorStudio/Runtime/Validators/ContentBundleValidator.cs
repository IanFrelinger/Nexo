using System;
using System.Threading;
using System.Threading.Tasks;
using NexoDirectorStudio.DTO;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Validator for content bundle validation.
    /// </summary>
    public class ContentBundleValidator : IValidator<ContentBundle>
    {
        public async Task<ValidationResult> ValidateAsync(ContentBundle item, CancellationToken cancellationToken)
        {
            // Basic implementation - in a real scenario this would validate content bundle
            await Task.Delay(50, cancellationToken); // Simulate async work
            
            return new ValidationResult
            {
                IsValid = true,
                Score = 80,
                Message = "Content bundle validation passed",
                ValidatedAt = DateTimeOffset.UtcNow
            };
        }
        
        public async Task<ValidationResult> ValidateAsync(object input, CancellationToken cancellationToken)
        {
            if (input is ContentBundle bundle)
                return await ValidateAsync(bundle, cancellationToken);
            
            return new ValidationResult
            {
                IsValid = false,
                Score = 0,
                Message = "Invalid input type for ContentBundle validation",
                ValidatedAt = DateTimeOffset.UtcNow
            };
        }
    }
}
