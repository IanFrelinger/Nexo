namespace Ashlar.Contracts;

/// <summary>Stable idempotency keys shared by in-memory and DynamoDB SMS approval stores.</summary>
public static class SmsIngressExternalIds
{
    /// <summary>
    /// Builds a stable idempotency key for SMS approval storage.
    /// Prefers <paramref name="messageSid"/> when present; otherwise hashes sender and token.
    /// </summary>
    /// <param name="from">Sender phone number or identifier.</param>
    /// <param name="approvalToken">Derived approval token.</param>
    /// <param name="messageSid">Optional provider message id.</param>
    public static string Build(string from, string approvalToken, string? messageSid) =>
        !string.IsNullOrWhiteSpace(messageSid)
            ? $"sid:{messageSid.Trim()}"
            : $"hash:{from}:{approvalToken}";
}
