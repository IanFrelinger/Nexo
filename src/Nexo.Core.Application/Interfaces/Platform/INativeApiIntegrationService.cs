using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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

    // Platform-specific models for Native API integration
    public class NativeApiIntegrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ApiName { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class PermissionHandlingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> GrantedPermissions { get; set; } = new();
        public List<string> DeniedPermissions { get; set; } = new();
    }

    public class NativeApiWrapper
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
    }

    public class NativeApiValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Additional Native API Integration models
    public class ApiAvailability
    {
        public string ApiName { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
        public string Version { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new();
    }

    public class PermissionResult
    {
        public string PermissionName { get; set; } = string.Empty;
        public bool IsGranted { get; set; }
        public string Reason { get; set; } = string.Empty;
        public List<string> RequiredActions { get; set; } = new();
    }

    public class PermissionStatus
    {
        public string PermissionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset LastChecked { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    public class ApiAvailabilityValidation
    {
        public string ApiName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    public class PermissionValidation
    {
        public string PermissionName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
    }

    public class ParameterValidation
    {
        public string ParameterName { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string ValidationRule { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
    }

    public class ErrorHandlingValidation
    {
        public string ErrorType { get; set; } = string.Empty;
        public bool IsHandled { get; set; }
        public string HandlingStrategy { get; set; } = string.Empty;
        public List<string> Recommendations { get; set; } = new();
    }
}
