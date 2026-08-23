using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Ashlar.Contracts;

namespace Ashlar.API.Middleware.Ingress;

/// <summary>Configuration options for validate ashlar middleware ingress.</summary>
public sealed class ValidateAshlarMiddlewareIngressOptions : IValidateOptions<AshlarMiddlewareIngressOptions>
{
    private readonly IHostEnvironment _environment;
    private readonly IOptionsMonitor<SmsIngressDynamoDbOptions> _dynamo;

    public ValidateAshlarMiddlewareIngressOptions(
        IHostEnvironment environment,
        IOptionsMonitor<SmsIngressDynamoDbOptions> dynamo)
    {
        _environment = environment;
        _dynamo = dynamo;
    }

    public ValidateOptionsResult Validate(string? name, AshlarMiddlewareIngressOptions options)
    {
        if (options.EnableAwsSnsSmsWebhook && !_environment.IsEnvironment("Testing"))
        {
            if (options.AwsSnsAllowedTopicArnPrefixes is null || options.AwsSnsAllowedTopicArnPrefixes.Length == 0)
            {
                return ValidateOptionsResult.Fail(
                    "When Ashlar:MiddlewareIngress:EnableAwsSnsSmsWebhook is true outside the Testing environment, "
                    + "AwsSnsAllowedTopicArnPrefixes must be non-empty (set explicit topic ARN prefixes).");
            }
        }

        if (string.Equals(options.SmsIngressApprovalStore, SmsIngressApprovalStoreKind.DynamoDb, StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(_dynamo.CurrentValue.TableName))
            {
                return ValidateOptionsResult.Fail(
                    "When Ashlar:MiddlewareIngress:SmsIngressApprovalStore is DynamoDb, Ashlar:SmsIngressDynamoDb:TableName must be set.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
