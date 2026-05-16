namespace Nexo.Contracts;

/// <summary>Configuration for <see cref="SmsIngressApprovalStoreKind.DynamoDb"/> backing store.</summary>
public sealed class SmsIngressDynamoDbOptions
{
    public const string SectionPath = "Nexo:SmsIngressDynamoDb";

    /// <summary>DynamoDB table with string partition key <c>PK</c> and sort key <c>SK</c>.</summary>
    public string TableName { get; set; } = "";
}
