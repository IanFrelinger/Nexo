using System;
using System.Collections.Generic;
using System.Linq;

namespace Nexo.Feature.Container.Models
{
    /// <summary>
    /// Represents a request to execute a command within a specified container.
    /// </summary>
    public sealed class ExecuteInContainerRequest
    {
        /// <summary>
        /// Gets the name of the container in which the command will be executed.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// Gets the command to be executed within the container.
        /// </summary>
        public string[] Command { get; }

        /// <summary>
        /// Gets the working directory in which the command should be executed.
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// Gets the environment variables to be used during the container execution.
        /// </summary>
        public IDictionary<string, string> EnvironmentVariables { get; }

        /// <summary>
        /// Gets or sets the execution timeout duration in milliseconds.
        /// </summary>
        public int TimeoutMs { get; }

        /// <summary>
        /// Represents a request to execute a specific command within a container.
        /// </summary>
        public ExecuteInContainerRequest(
            string containerName,
            string[] command,
            string? workingDirectory = null,
            IDictionary<string, string>? environmentVariables = null,
            int timeoutMs = 30000)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or whitespace", nameof(containerName));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            
            if (command.Length == 0)
                throw new ArgumentException("Command cannot be empty", nameof(command));
                
            if (timeoutMs <= 0)
                throw new ArgumentException("Timeout must be greater than zero", nameof(timeoutMs));

            ContainerName = containerName;
            Command = command;
            WorkingDirectory = workingDirectory ?? string.Empty;
            EnvironmentVariables = environmentVariables ?? new Dictionary<string, string>();
            TimeoutMs = timeoutMs;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ExecuteInContainerRequest"/> class with the specified container name and command.
        /// </summary>
        /// <param name="containerName">The name of the container where the command will be executed.</param>
        /// <param name="command">The command and its arguments to execute inside the container.</param>
        /// <returns>A new <see cref="ExecuteInContainerRequest"/> instance configured with the provided parameters.</returns>
        public static ExecuteInContainerRequest Create(string containerName, params string[] command) =>
            new ExecuteInContainerRequest(containerName, command);

        /// <summary>
        /// Gets the full command string by joining all elements of the command array with a space separator.
        /// </summary>
        /// <returns>The complete command as a single string.</returns>
        public string GetFullCommand() => string.Join(" ", Command);
    }
}