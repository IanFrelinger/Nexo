namespace Ashlar.Contracts;

/// <summary>Configuration for <see cref="SmsIngressApprovalStoreKind.DynamoDb"/> backing store.</summary>
public sealed class SmsIngressDynamoDbOptions
{
    /// <summary>Configuration section path bound from <c>appsettings.json</c>.</summary>
    public const string SectionPath = "Ashlar:SmsIngressDynamoDb";

    /// <summary>DynamoDB table with string partition key <c>PK</c> and sort key <c>SK</c>.</summary>
    public string TableName { get; set; } = "";
}
