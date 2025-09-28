using Nexo.Shared.Values;

namespace Nexo.Shared.Constants
{
    /// <summary>
    /// Centralized type values for the framework - now using interface-based types instead of enums
    /// </summary>
    public static class CentralizedTypes
    {
        /// <summary>
        /// Operation result status values
        /// </summary>
        public static class OperationStatuses
        {
            public static readonly OperationStatus Success = OperationStatus.Success;
            public static readonly OperationStatus Failure = OperationStatus.Failure;
            public static readonly OperationStatus PartialSuccess = OperationStatus.PartialSuccess;
            public static readonly OperationStatus Cancelled = OperationStatus.Cancelled;
            public static readonly OperationStatus Timeout = OperationStatus.Timeout;
        }

        /// <summary>
        /// Validation severity level values
        /// </summary>
        public static class ValidationSeverities
        {
            public static readonly ValidationSeverity Info = ValidationSeverity.Info;
            public static readonly ValidationSeverity Warning = ValidationSeverity.Warning;
            public static readonly ValidationSeverity Error = ValidationSeverity.Error;
            public static readonly ValidationSeverity Critical = ValidationSeverity.Critical;
        }

        /// <summary>
        /// Log level values
        /// </summary>
        public static class LogLevels
        {
            public static readonly LogLevel Trace = LogLevel.Trace;
            public static readonly LogLevel Debug = LogLevel.Debug;
            public static readonly LogLevel Information = LogLevel.Information;
            public static readonly LogLevel Warning = LogLevel.Warning;
            public static readonly LogLevel Error = LogLevel.Error;
            public static readonly LogLevel Critical = LogLevel.Critical;
        }

        /// <summary>
        /// Priority level values
        /// </summary>
        public static class Priorities
        {
            public static readonly Priority Low = Priority.Low;
            public static readonly Priority Normal = Priority.Normal;
            public static readonly Priority High = Priority.High;
            public static readonly Priority Critical = Priority.Critical;
            public static readonly Priority Emergency = Priority.Emergency;
        }

        /// <summary>
        /// Status values
        /// </summary>
        public static class Statuses
        {
            public static readonly Status Active = Status.Active;
            public static readonly Status Inactive = Status.Inactive;
            public static readonly Status Pending = Status.Pending;
            public static readonly Status Completed = Status.Completed;
            public static readonly Status Failed = Status.Failed;
            public static readonly Status Cancelled = Status.Cancelled;
            public static readonly Status Running = Status.Running;
            public static readonly Status Stopped = Status.Stopped;
        }

        /// <summary>
        /// Operation type values
        /// </summary>
        public static class OperationTypes
        {
            public static readonly OperationType Create = OperationType.Create;
            public static readonly OperationType Read = OperationType.Read;
            public static readonly OperationType Update = OperationType.Update;
            public static readonly OperationType Delete = OperationType.Delete;
            public static readonly OperationType Execute = OperationType.Execute;
            public static readonly OperationType Validate = OperationType.Validate;
            public static readonly OperationType Process = OperationType.Process;
            public static readonly OperationType Initialize = OperationType.Initialize;
            public static readonly OperationType Shutdown = OperationType.Shutdown;
        }

        /// <summary>
        /// Error type values
        /// </summary>
        public static class ErrorTypes
        {
            public static readonly ErrorType Validation = ErrorType.Validation;
            public static readonly ErrorType Business = ErrorType.Business;
            public static readonly ErrorType System = ErrorType.System;
            public static readonly ErrorType Network = ErrorType.Network;
            public static readonly ErrorType Timeout = ErrorType.Timeout;
            public static readonly ErrorType Authentication = ErrorType.Authentication;
            public static readonly ErrorType Authorization = ErrorType.Authorization;
            public static readonly ErrorType NotFound = ErrorType.NotFound;
            public static readonly ErrorType Conflict = ErrorType.Conflict;
            public static readonly ErrorType Internal = ErrorType.Internal;
        }
    }
}
