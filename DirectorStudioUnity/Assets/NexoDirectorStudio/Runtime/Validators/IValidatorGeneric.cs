using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexoDirectorStudio.Validators
{
    /// <summary>
    /// Generic validator interface for type-safe validation.
    /// </summary>
    /// <typeparam name="T">The type of input to validate</typeparam>
    public interface IValidator<T> : IValidator
    {
        /// <summary>
        /// Validates the given input and returns a validation result.
        /// </summary>
        /// <param name="input">The input to validate</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<ValidationResult> ValidateAsync(T input, CancellationToken ct);
    }
}
