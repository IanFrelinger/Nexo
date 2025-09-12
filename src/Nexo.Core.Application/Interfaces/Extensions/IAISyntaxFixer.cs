using System.Threading.Tasks;
using Nexo.Core.Domain.Composition;

namespace Nexo.Core.Application.Interfaces.Extensions
{
    /// <summary>
    /// Interface for AI-powered syntax fixing of generated extension code
    /// </summary>
    public interface IAISyntaxFixer
    {
        /// <summary>
        /// Attempts to fix syntax errors in the provided C# code
        /// </summary>
        /// <param name="code">The C# code with potential syntax errors</param>
        /// <returns>Result containing the fixed code and information about applied fixes</returns>
        Task<SyntaxFixResult> FixSyntaxAsync(string code);
    }

    /// <summary>
    /// Result of syntax fixing operation
    /// </summary>
    public class SyntaxFixResult
    {
        /// <summary>
        /// Gets whether the syntax fixing was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Gets the fixed code (or original code if no fixes were needed)
        /// </summary>
        public string FixedCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets the list of fixes that were applied
        /// </summary>
        public List<string> FixesApplied { get; set; } = new List<string>();

        /// <summary>
        /// Gets any errors that occurred during the fixing process
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Gets the original validation result that triggered the fixing
        /// </summary>
        public ValidationResult? OriginalValidationResult { get; set; }

        /// <summary>
        /// Gets the validation result after applying fixes
        /// </summary>
        public ValidationResult? FinalValidationResult { get; set; }

        /// <summary>
        /// Gets the number of fix attempts made
        /// </summary>
        public int FixAttempts { get; set; }

        /// <summary>
        /// Gets the maximum number of fix attempts allowed
        /// </summary>
        public int MaxFixAttempts { get; set; } = 3;
    }
}
