using System;

namespace Nexo.Core.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when a project is not found.
    /// </summary>
    public sealed class ProjectNotFoundException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ProjectNotFoundException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectNotFoundException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProjectNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Exception thrown when a container operation fails.
    /// </summary>
    public sealed class ContainerOperationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOperationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ContainerOperationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerOperationException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ContainerOperationException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates a container start failure exception.
        /// </summary>
        /// <param name="serviceName">The service name that failed to start.</param>
        /// <param name="error">The error message.</param>
        /// <returns>A new container operation exception.</returns>
        public static ContainerOperationException StartFailed(string serviceName, string error) =>
            new ContainerOperationException($"Failed to start container service '{serviceName}': {error}");

        /// <summary>
        /// Creates a container stop failure exception.
        /// </summary>
        /// <param name="serviceName">The service name that failed to stop.</param>
        /// <param name="error">The error message.</param>
        /// <returns>A new container operation exception.</returns>
        public static ContainerOperationException StopFailed(string serviceName, string error) =>
            new ContainerOperationException($"Failed to stop container service '{serviceName}': {error}");
    }
}