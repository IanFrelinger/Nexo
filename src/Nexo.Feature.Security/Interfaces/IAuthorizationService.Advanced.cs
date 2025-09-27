using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nexo.Feature.Security.Interfaces;

/// <summary>
/// Advanced authorization functionality
/// </summary>
public partial interface IAuthorizationService
{
    /// <summary>
    /// Evaluates dynamic permissions based on context
    /// </summary>
    /// <param name="user">User information</param>
    /// <param name="resource">Resource to access</param>
    /// <param name="action">Action to perform</param>
    /// <param name="context">Dynamic context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dynamic authorization result</returns>
    Task<DynamicAuthorizationResult> EvaluateDynamicPermissionAsync(UserInfo user, string resource, string action, Dictionary<string, object> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authorization configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization configuration</returns>
    Task<AuthorizationConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}
