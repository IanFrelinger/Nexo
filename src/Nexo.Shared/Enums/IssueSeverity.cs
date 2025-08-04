namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents error-level severity for a code issue.
    /// </summary>
    /// <remarks>
    /// Issues with this severity indicate problems that prevent the code
    /// from functioning as intended or causing significant failures.
    /// </remarks>
    public enum IssueSeverity
    {
        /// <summary>
        /// Represents an informational severity level for a code issue.
        /// </summary>
        /// <remarks>
        /// Info severity indicates non-critical observations or suggestions
        /// that do not impact the functionality of the code.
        /// </remarks>
        Info = 0,

        /// <summary>
        /// Represents warning-level severity for a code issue.
        /// </summary>
        /// <remarks>
        /// Warning severity indicates potential issues that may not cause immediate errors
        /// but could lead to unintended behavior or problems if not addressed.
        /// </remarks>
        Warning = 1,

        /// <summary>
        /// Represents error-level severity for a code issue.
        /// </summary>
        /// <remarks>
        /// Issues with this severity indicate problems that prevent the code
        /// from functioning as intended or cause significant failures.
        /// </remarks>
        Error = 2,

        /// <summary>
        /// Represents a critical severity level for a code issue.
        /// </summary>
        /// <remarks>
        /// Critical severity indicates severe issues that can cause immediate
        /// and complete failure of the application or system, requiring urgent attention.
        /// </remarks>
        Critical = 3
    }
}