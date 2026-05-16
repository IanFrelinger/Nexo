namespace Nexo.Contracts;

/// <summary>Stable idempotency keys shared by in-memory and DynamoDB SMS approval stores.</summary>
public static class SmsIngressExternalIds
{
    public static string Build(string from, string approvalToken, string? messageSid) =>
        !string.IsNullOrWhiteSpace(messageSid)
            ? $"sid:{messageSid.Trim()}"
            : $"hash:{from}:{approvalToken}";
}
