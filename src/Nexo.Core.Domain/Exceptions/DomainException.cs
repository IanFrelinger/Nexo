using System;

namespace Nexo.Core.Domain.Exceptions
{
    /// <summary>
    /// Base exception for all domain-specific errors.
    /// </summary>
    public abstract class DomainException : Exception
    {
        /// <summary>
        /// Gets the unique error code that represents the specific domain-related error.
        /// </summary>
        public string ErrorCode { get; }

        /// <summary>
        /// Base exception for all domain-specific errors.
        /// </summary>
        protected DomainException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode ?? "DOMAIN_ERROR";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        /// <param name="message">
        ///     The error message.
        ///     The error message.
        ///     The error message.
        ///     The error message.
        ///     The error message.
        /// </param>
        /// <param name="errorCode">
        ///     The error code.
        ///     The error code.
        /// </param>
        /// <summary>
        /// Initializes a new instance of the <see cref="DomainException"/> class.
        /// </summary>
        /// <param name="innerException">
        ///     The inner exception.
        ///     The inner exception.
        /// </param>
        /// <summary>
        /// Exception thrown when a container operation fails.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOperationException"/> class.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOperationException"/> class.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOperationException"/> class.
        /// </summary>
        /// <summary>
        /// Creates a <see cref="ContainerOperationException"/> indicating a failure to start a container.
        /// </summary>
        /// <returns>An instance of <see cref="ContainerOperationException"/>.</returns>
        /// <summary>
        /// Creates a <see cref="ContainerOperationException"/> indicating a failure to stop a container.
        /// </summary>
        /// <returns>An instance of <see cref="ContainerOperationException"/>.</returns>
        protected DomainException(string message, string errorCode, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode ?? "DOMAIN_ERROR";
        }
    }
}