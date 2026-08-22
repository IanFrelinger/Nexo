using Amazon.DynamoDBv2;
using Microsoft.Extensions.DependencyInjection;
using Ashlar.Contracts;

namespace Ashlar.Ingress.DynamoDb;

public static class SmsIngressDynamoDbServiceCollectionExtensions
{
    /// <summary>Registers <see cref="DynamoDbSmsIngressApprovalStore"/> and a default <see cref="IAmazonDynamoDB"/> client.</summary>
    public static IServiceCollection AddDynamoDbSmsIngressApprovalStore(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient());
        services.AddSingleton<ISmsIngressApprovalStore, DynamoDbSmsIngressApprovalStore>();
        return services;
    }
}
