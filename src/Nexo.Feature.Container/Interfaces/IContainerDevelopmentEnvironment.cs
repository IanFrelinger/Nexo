using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Nexo.Core.Application.Enums;
using Nexo.Shared.Models;
using Nexo.Core.Domain.ValueObjects;
using Nexo.Feature.Container.Models;

namespace Nexo.Feature.Container.Interfaces
{
    /// <summary>
    /// Defines the contract for container-based development environments that provide
    /// isolated execution contexts for development workflows.
    /// </summary>
    public interface IContainerDevelopmentEnvironment
    {
        /// <summary>
        /// Gets the container runtime being used by this environment.
        /// </summary>
        ContainerRuntime Runtime { get; }

        /// <summary>
        /// Gets the current active session, if any.
        /// </summary>
        DevelopmentSession CurrentSession { get; }

        /// <summary>
        /// Initializes the development environment and prepares it for use.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the completion of initialization.</returns>
        Task InitializeAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Creates a new development session with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration for the development session.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the created development session.</returns>
        Task<DevelopmentSession> CreateSessionAsync(SessionConfiguration configuration, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Switches to an existing development session.
        /// </summary>
        /// <param name="sessionId">The ID of the session to switch to.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the completion of the session switch.</returns>
        Task SwitchToSessionAsync(string sessionId, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Ends the current development session and cleans up resources.
        /// </summary>
        /// <param name="saveChanges">Whether to save changes before ending the session.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the completion of session termination.</returns>
        Task EndSessionAsync(bool saveChanges, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a command within the current development session.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workingDirectory">The working directory for command execution.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the execution result.</returns>
        Task<ExecutionResult> ExecuteCommandAsync(string command, string workingDirectory, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Executes a command with streaming output callbacks.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="workingDirectory">The working directory for command execution.</param>
        /// <param name="outputCallback">Callback for standard output lines.</param>
        /// <param name="errorCallback">Callback for error output lines.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the execution result.</returns>
        Task<ExecutionResult> ExecuteCommandWithCallbacksAsync(
            string command,
            string workingDirectory,
            Action<string> outputCallback,
            Action<string> errorCallback,
            CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Implements a feature within the current development session.
        /// </summary>
        /// <param name="feature">The feature specification to implement.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the implementation result.</returns>
        Task<ImplementationResult> ImplementFeatureAsync(FeatureSpecification feature, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Runs tests within the current development session.
        /// </summary>
        /// <param name="testConfiguration">The test configuration to use.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the test results.</returns>
        Task<TestResult> RunTestsAsync(TestConfiguration testConfiguration, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Builds the project within the current development session.
        /// </summary>
        /// <param name="buildConfiguration">The build configuration to use.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the build result.</returns>
        Task<BuildResult> BuildProjectAsync(BuildConfiguration buildConfiguration, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Synchronizes files between the host and the container session.
        /// </summary>
        /// <param name="direction">The direction of synchronization.</param>
        /// <param name="paths">The paths to synchronize.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the synchronization result.</returns>
        Task<SynchronizationResult> SynchronizeFilesAsync(SyncDirection direction, List<string> paths, CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Gets the current status of the development environment.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing the environment status.</returns>
        Task<EnvironmentStatus> GetStatusAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Lists all available development sessions.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task containing a list of available sessions.</returns>
        Task<List<DevelopmentSession>> ListSessionsAsync(CancellationToken cancellationToken = default(CancellationToken));

        /// <summary>
        /// Cleans up unused resources and temporary files.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>A task representing the completion of cleanup.</returns>
        Task CleanupAsync(CancellationToken cancellationToken = default(CancellationToken));
    }
}