namespace Nexo.Contracts;

/// <summary>Values for <c>Nexo:MiddlewareIngress:SmsIngressApprovalStore</c>.</summary>
public static class SmsIngressApprovalStoreKind
{
    /// <summary>In-process memory store suitable for development and single-node hosts.</summary>
    public const string Memory = "Memory";

    /// <summary>DynamoDB-backed durable store for multi-instance API hosts.</summary>
    public const string DynamoDb = "DynamoDb";
}
