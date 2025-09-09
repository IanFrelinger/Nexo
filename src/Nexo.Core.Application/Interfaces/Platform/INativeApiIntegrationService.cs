using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Nexo.Core.Application.Models.Platform;

namespace Nexo.Core.Application.Interfaces.Platform
{
    /// <summary>
    /// Interface for native API integration service.
    /// Provides framework for integrating with native platform APIs and handling permissions.
    /// </summary>
    public interface INativeApiIntegrationService
    {
        /// <summary>
        /// Integrates with native platform APIs.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="apiName">The name of the API to integrate with</param>
        /// <param name="parameters">The parameters for the API integration</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Native API integration result</returns>
        Task<NativeApiIntegrationResult> IntegrateWithNativeApiAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles native platform permissions.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="permissions">The permissions to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Permission handling result</returns>
        Task<PermissionHandlingResult> HandlePermissionsAsync(
            string platform,
            IEnumerable<string> permissions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates native API wrapper code.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="apiName">The name of the API to wrap</param>
        /// <param name="parameters">The parameters for the API wrapper</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Native API wrapper</returns>
        Task<NativeApiWrapper> GenerateApiWrapperAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates native API integration.
        /// </summary>
        /// <param name="platform">The target platform</param>
        /// <param name="apiName">The name of the API to validate</param>
        /// <param name="parameters">The parameters for the API validation</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Native API validation result</returns>
        Task<NativeApiValidationResult> ValidateIntegrationAsync(
            string platform,
            string apiName,
            Dictionary<string, object> parameters,
            CancellationToken cancellationToken = default);
    }
}
