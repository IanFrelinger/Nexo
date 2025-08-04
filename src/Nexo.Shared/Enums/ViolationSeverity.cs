namespace Nexo.Core.Application.Enums
{
    /// <summary>
    /// Represents the most severe level of a compliance violation.
    /// Requires immediate action due to critical impact or risk.
    /// </summary>
    public enum ViolationSeverity
    {
        /// <summary>
        /// Represents a violation of minor severity.
        /// Indicates low-risk or less significant compliance issues that may not require immediate action.
        /// </summary>
        Minor = 0,

        /// <summary>
        /// Represents a violation of major severity.
        /// Denotes significant compliance issues that pose considerable risk or impact, requiring timely resolution.
        /// </summary>
        Major = 1,

        /// <summary>
        /// Represents a critical severity level of a compliance violation.
        /// Indicates the highest level of risk or impact requiring immediate corrective actions.
        /// </summary>
        Critical = 2
    }
}