namespace Nexo.Shared.Constants
{
    /// <summary>
    /// Centralized enums for the framework
    /// </summary>
    public static class CentralizedEnums
    {
        /// <summary>
        /// Operation result status
        /// </summary>
        public enum OperationStatus
        {
            Success,
            Failure,
            PartialSuccess,
            Cancelled,
            Timeout
        }

        /// <summary>
        /// Validation severity levels
        /// </summary>
        public enum ValidationSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Log levels
        /// </summary>
        public enum LogLevel
        {
            Trace,
            Debug,
            Information,
            Warning,
            Error,
            Critical
        }

        /// <summary>
        /// Priority levels
        /// </summary>
        public enum Priority
        {
            Low,
            Normal,
            High,
            Critical,
            Emergency
        }

        /// <summary>
        /// Status values
        /// </summary>
        public enum Status
        {
            Active,
            Inactive,
            Pending,
            Completed,
            Failed,
            Cancelled,
            Running,
            Stopped
        }

        /// <summary>
        /// Operation types
        /// </summary>
        public enum OperationType
        {
            Create,
            Read,
            Update,
            Delete,
            Execute,
            Validate,
            Process,
            Initialize,
            Shutdown
        }

        /// <summary>
        /// Error types
        /// </summary>
        public enum ErrorType
        {
            Validation,
            Business,
            System,
            Network,
            Timeout,
            Authentication,
            Authorization,
            NotFound,
            Conflict,
            Internal
        }
    }
}
