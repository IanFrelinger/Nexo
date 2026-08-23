namespace Ashlar.Core.Application.Common.Ports;

/// <summary>
/// Abstraction for HTTP webhook calls in workflows.
/// Implementations live in Infrastructure.
/// </summary>
public interface IWorkflowWebhookClient
{
    Task<string> GetAsync(string url, CancellationToken ct = default);
    Task PostAsync(string url, object data, CancellationToken ct = default);
}
