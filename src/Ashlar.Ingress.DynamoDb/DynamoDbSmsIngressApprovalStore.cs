using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ashlar.Contracts;

namespace Ashlar.Ingress.DynamoDb;

/// <summary>
/// Idempotent SMS YES approvals stored in DynamoDB (<c>pk</c> partition key, <c>sk</c> sort key).
/// </summary>
public sealed class DynamoDbSmsIngressApprovalStore : ISmsIngressApprovalStore
{
    public const string PartitionKeyValue = "AshlarSmsIngress";
    public const string AttributePartitionKey = "pk";
    public const string AttributeSortKey = "sk";

    private readonly IAmazonDynamoDB _dynamo;
    private readonly IOptionsMonitor<SmsIngressDynamoDbOptions> _options;
    private readonly ILogger<DynamoDbSmsIngressApprovalStore> _logger;

    public DynamoDbSmsIngressApprovalStore(
        IAmazonDynamoDB dynamo,
        IOptionsMonitor<SmsIngressDynamoDbOptions> options,
        ILogger<DynamoDbSmsIngressApprovalStore> logger)
    {
        _dynamo = dynamo;
        _options = options;
        _logger = logger;
    }

    public async Task<SmsInboundSimulationResponse> TryRecordApprovalAsync(
        string from,
        string approvalToken,
        string? messageSid,
        CancellationToken cancellationToken)
    {
        var table = _options.CurrentValue.TableName.Trim();
        if (string.IsNullOrEmpty(table))
            throw new InvalidOperationException("SmsIngressDynamoDb:TableName is not configured.");

        var externalId = SmsIngressExternalIds.Build(from, approvalToken, messageSid);
        var key = BuildKey(externalId);

        try
        {
            await _dynamo.PutItemAsync(
                new PutItemRequest
                {
                    TableName = table,
                    Item = new Dictionary<string, AttributeValue>
                    {
                        [AttributePartitionKey] = new AttributeValue { S = PartitionKeyValue },
                        [AttributeSortKey] = new AttributeValue { S = externalId },
                        ["from"] = new AttributeValue { S = from },
                        ["approvalToken"] = new AttributeValue { S = approvalToken },
                        ["recordedUtc"] = new AttributeValue { S = DateTime.UtcNow.ToString("O") },
                    },
                    ExpressionAttributeNames = new Dictionary<string, string> { ["#sk"] = AttributeSortKey },
                    ConditionExpression = "attribute_not_exists(#sk)",
                },
                cancellationToken).ConfigureAwait(false);

            return new SmsInboundSimulationResponse(true, approvalToken, "Recorded YES approval.", false);
        }
        catch (ConditionalCheckFailedException)
        {
            var existing = await _dynamo.GetItemAsync(
                new GetItemRequest { TableName = table, Key = key, ConsistentRead = true },
                cancellationToken).ConfigureAwait(false);

            if (!existing.IsItemSet || !existing.Item.TryGetValue("approvalToken", out var tokenAttr))
            {
                _logger.LogWarning("DynamoDB conditional check failed but item is missing for {ExternalId}.", externalId);
                return new SmsInboundSimulationResponse(true, approvalToken, "Recorded YES approval.", true);
            }

            var priorToken = tokenAttr.S ?? approvalToken;
            return new SmsInboundSimulationResponse(true, priorToken, "Recorded YES approval.", true);
        }
    }

    private static Dictionary<string, AttributeValue> BuildKey(string externalId) =>
        new()
        {
            [AttributePartitionKey] = new AttributeValue { S = PartitionKeyValue },
            [AttributeSortKey] = new AttributeValue { S = externalId },
        };
}
