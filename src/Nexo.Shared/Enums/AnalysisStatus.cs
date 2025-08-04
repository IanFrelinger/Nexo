namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Indicates that the analysis operation was cancelled before completion.
    /// </summary>
    /// <remarks>
    /// This status is used when the analysis process is intentionally interrupted or halted
    /// prior to its conclusion, typically due to a user-requested cancellation or system intervention.
    /// </remarks>
    public enum AnalysisStatus
    {
        /// <summary>
        /// Indicates that the analysis operation completed successfully without any errors or issues.
        /// </summary>
        /// <remarks>
        /// This status is used when the entire analysis process finishes as expected, meeting all
        /// criteria and producing valid, error-free results.
        /// </remarks>
        Success = 0,

        /// <summary>
        /// Indicates that the analysis operation completed partially, with some components or tasks succeeding while others failed or were skipped.
        /// </summary>
        /// <remarks>
        /// This status is used when the analysis process finishes, but not all criteria are fully met.
        /// It reflects a combination of successful and unsuccessful outcomes within the operation.
        /// </remarks>
        PartialSuccess = 1,

        /// <summary>
        /// Indicates that the analysis operation did not complete successfully due to errors or issues.
        /// </summary>
        /// <remarks>
        /// This status is used when the analysis process encounters conditions that prevent it
        /// from producing valid results or reaching a successful conclusion. Possible reasons
        /// could include system errors, unexpected input data, or other failures during execution.
        /// </remarks>
        Failed = 2,

        /// <summary>
        /// Indicates that the analysis operation was cancelled before completion.
        /// </summary>
        /// <remarks>
        /// This status is used when the analysis process is intentionally interrupted or halted
        /// prior to its conclusion, typically due to a user-requested cancellation or system intervention.
        /// </remarks>
        Cancelled = 3
    }
}