using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nexo.Infrastructure.Services.Platform.Integrators;

/// <summary>
/// Interface generation functionality
/// </summary>
public partial class ApiWrapperGenerator
{
    private async Task<string> GenerateWrapperInterfaceAsync(string platform, string apiName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Generating wrapper interface for: {ApiName}", apiName);

            await Task.Delay(100, cancellationToken);

            var interfaceCode = $@"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace {GetNamespaceForPlatform(platform)}
{{
    /// <summary>
    /// Interface for {apiName} API wrapper
    /// </summary>
    public interface I{apiName}Wrapper
    {{
        /// <summary>
        /// Calls the {apiName} API with the specified parameters
        /// </summary>
        Task<object> CallApiAsync(Dictionary<string, object> parameters);

        /// <summary>
        /// Checks if the {apiName} API is available on the current platform
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Checks if the required permissions are granted for the {apiName} API
        /// </summary>
        Task<PermissionStatus> CheckPermissionsAsync();

        /// <summary>
        /// Gets the version of the {apiName} API
        /// </summary>
        Task<string> GetApiVersionAsync();

        /// <summary>
        /// Gets the capabilities of the {apiName} API
        /// </summary>
        Task<List<string>> GetCapabilitiesAsync();
    }}
}}";

            return interfaceCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating wrapper interface");
            return $"// Error generating interface: {ex.Message}";
        }
    }
}
