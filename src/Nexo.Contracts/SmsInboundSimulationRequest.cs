namespace Nexo.Contracts;

/// <summary>Simulated inbound SMS payload for local approval-flow testing.</summary>
/// <param name="From">Sender phone number or identifier.</param>
/// <param name="Body">Message body; <c>YES</c> responses trigger approval recording.</param>
/// <param name="MessageSid">Optional provider message id used for idempotent replay.</param>
/// <param name="AppId">Optional application id binding the approval to a tenant app.</param>
public sealed record SmsInboundSimulationRequest(
    string From,
    string Body,
    string? MessageSid = null,
    string? AppId = null);
