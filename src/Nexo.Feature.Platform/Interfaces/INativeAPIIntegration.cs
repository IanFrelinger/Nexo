using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Feature.Platform.Models;
using Nexo.Feature.Platform.Enums;
using Nexo.Core.Application.Enums;

namespace Nexo.Feature.Platform.Interfaces
{
    /// <summary>
    /// Interface for native API integration across different platforms.
    /// Part of Epic 6.2: Platform-Specific Feature Integration, Story 6.2.2: Native API Integration.
    /// </summary>
    public interface INativeAPIIntegration
    {
        /// <summary>
        /// Initializes the native API integration for a specific platform.
        /// </summary>
        /// <param name="platformType">The target platform</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Initialization result</returns>
        Task<NativeAPIInitializationResult> InitializeAsync(PlatformType platformType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a native API call with the specified parameters.
        /// </summary>
        /// <param name="apiName">The name of the API to call</param>
        /// <param name="parameters">The parameters for the API call</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API call result</returns>
        Task<NativeAPICallResult> ExecuteAPICallAsync(string apiName, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a specific native API is available on the current platform.
        /// </summary>
        /// <param name="apiName">The name of the API to check</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API availability result</returns>
        Task<NativeAPIAvailabilityResult> CheckAPIAvailabilityAsync(string apiName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available native APIs for the current platform.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Available APIs result</returns>
        Task<AvailableAPIsResult> GetAvailableAPIsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests permissions for a specific API.
        /// </summary>
        /// <param name="apiName">The API name</param>
        /// <param name="permissionType">The type of permission needed</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Permission request result</returns>
        Task<PermissionRequestResult> RequestPermissionAsync(string apiName, PermissionType permissionType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks the current permission status for a specific API.
        /// </summary>
        /// <param name="apiName">The API name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Permission status result</returns>
        Task<PermissionStatusResult> CheckPermissionStatusAsync(string apiName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a custom API handler for platform-specific functionality.
        /// </summary>
        /// <param name="apiName">The API name</param>
        /// <param name="handler">The custom handler</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Registration result</returns>
        Task<APIHandlerRegistrationResult> RegisterAPIHandlerAsync(string apiName, INativeAPIHandler handler, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unregisters a custom API handler.
        /// </summary>
        /// <param name="apiName">The API name</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unregistration result</returns>
        Task<APIHandlerRegistrationResult> UnregisterAPIHandlerAsync(string apiName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the API abstraction layer for cross-platform compatibility.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API abstraction layer result</returns>
        Task<APIAbstractionLayerResult> GetAPIAbstractionLayerAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates API compatibility across different platforms.
        /// </summary>
        /// <param name="apis">The APIs to validate</param>
        /// <param name="platforms">The platforms to check against</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API compatibility validation result</returns>
        Task<APICompatibilityResult> ValidateAPICompatibilityAsync(IEnumerable<string> apis, IEnumerable<PlatformType> platforms, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disposes of the native API integration and releases resources.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Disposal result</returns>
        Task<NativeAPIDisposalResult> DisposeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Interface for custom native API handlers.
    /// </summary>
    public interface INativeAPIHandler
    {
        /// <summary>
        /// Handles a native API call.
        /// </summary>
        /// <param name="parameters">The API call parameters</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>API call result</returns>
        Task<NativeAPICallResult> HandleAPICallAsync(Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the API handler metadata.
        /// </summary>
        /// <returns>Handler metadata</returns>
        APIHandlerMetadata GetMetadata();
    }
} 