using Microsoft.Extensions.Logging;

namespace Nexo.BackgroundAgents.HostRunners;

/// <summary>
/// Shared failure handling for background agent runner adapters: log exception and return message for run-result Summary.
/// </summary>
internal static class BackgroundAgentAdapterFailure
{
    /// <summary>
    /// Logs the exception as a warning and returns the message for use as run-result Summary.
    /// </summary>
    public static string LogAndMessage(ILogger logger, Exception ex, string message)
    {
        logger.LogWarning(ex, "{Message}", message);
        return message;
    }
}
