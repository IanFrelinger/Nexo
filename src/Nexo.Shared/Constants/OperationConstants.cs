namespace Nexo.Shared.Constants
{
    /// <summary>
    /// Common operation types
    /// </summary>
    public static class Operations
    {
        public const string Create = "Create";
        public const string Read = "Read";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Execute = "Execute";
        public const string Validate = "Validate";
        public const string Process = "Process";
        public const string Initialize = "Initialize";
        public const string Shutdown = "Shutdown";
    }

    /// <summary>
    /// Common error codes
    /// </summary>
    public static class ErrorCodes
    {
        public const string ValidationError = "VALIDATION_ERROR";
        public const string BusinessError = "BUSINESS_ERROR";
        public const string SystemError = "SYSTEM_ERROR";
        public const string NetworkError = "NETWORK_ERROR";
        public const string TimeoutError = "TIMEOUT_ERROR";
        public const string AuthenticationError = "AUTHENTICATION_ERROR";
        public const string AuthorizationError = "AUTHORIZATION_ERROR";
        public const string NotFoundError = "NOT_FOUND_ERROR";
        public const string ConflictError = "CONFLICT_ERROR";
        public const string InternalError = "INTERNAL_ERROR";
    }

    /// <summary>
    /// Common configuration keys
    /// </summary>
    public static class ConfigurationKeys
    {
        public const string Environment = "Environment";
        public const string LogLevel = "LogLevel";
        public const string ConnectionString = "ConnectionString";
        public const string ApiKey = "ApiKey";
        public const string Timeout = "Timeout";
        public const string RetryCount = "RetryCount";
        public const string MaxConcurrency = "MaxConcurrency";
        public const string EnableMetrics = "EnableMetrics";
        public const string EnableTracing = "EnableTracing";
    }

    /// <summary>
    /// Common metadata keys
    /// </summary>
    public static class MetadataKeys
    {
        public const string CorrelationId = "CorrelationId";
        public const string RequestId = "RequestId";
        public const string UserId = "UserId";
        public const string TenantId = "TenantId";
        public const string Source = "Source";
        public const string Version = "Version";
        public const string Timestamp = "Timestamp";
        public const string Duration = "Duration";
        public const string Status = "Status";
        public const string ErrorCode = "ErrorCode";
    }
}
