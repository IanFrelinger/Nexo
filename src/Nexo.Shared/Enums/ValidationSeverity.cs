namespace Nexo.Shared.Enums
{
    /// <summary>
    /// Defines the severity levels for validation errors.
    /// </summary>
    public enum ValidationSeverity
    {
        /// <summary>
        /// Informational message.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning that should be addressed.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error that prevents operation.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical error that indicates a serious problem.
        /// </summary>
        Critical = 3
    }
}