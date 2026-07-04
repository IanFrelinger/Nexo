using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nexo.Contracts;

namespace Nexo.API.Middleware.Ingress;

/// <summary>Configuration options for validate nexo middleware ingress.</summary>
public sealed class ValidateNexoMiddlewareIngressOptions : IValidateOptions<NexoMiddlewareIngressOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly IOptionsMonitor<SmsIngressDynamoDbOptions> _dynamo;

    public ValidateNexoMiddlewareIngressOptions(
        IHostEnvironment environment,
        IOptionsMonitor<SmsIngressDynamoDbOptions> dynamo)
    {
        _environment = environment;
        _dynamo = dynamo;
    }

    public ValidateOptionsResult Validate(string? name, NexoMiddlewareIngressOptions options)
    {
        if (options.EnableAwsSnsSmsWebhook && !_environment.IsEnvironment("Testing"))
        {
            if (options.AwsSnsAllowedTopicArnPrefixes is null || options.AwsSnsAllowedTopicArnPrefixes.Length == 0)
            {
                return ValidateOptionsResult.Fail(
                    "When Nexo:MiddlewareIngress:EnableAwsSnsSmsWebhook is true outside the Testing environment, "
                    + "AwsSnsAllowedTopicArnPrefixes must be non-empty (set explicit topic ARN prefixes).");
            }
        }

        if (string.Equals(options.SmsIngressApprovalStore, SmsIngressApprovalStoreKind.DynamoDb, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_dynamo.CurrentValue.TableName))
            {
                return ValidateOptionsResult.Fail(
                    "When Nexo:MiddlewareIngress:SmsIngressApprovalStore is DynamoDb, Nexo:SmsIngressDynamoDb:TableName must be set.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
