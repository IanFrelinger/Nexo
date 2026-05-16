using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nexo.Contracts;
using Nexo.Ingress.DynamoDb;
using Testcontainers.DynamoDb;
using Xunit;
using Xunit.Abstractions;

namespace Nexo.Tests.Infrastructure.Tests.Ingress;

/// <summary>
/// Optional integration tests against DynamoDB Local in Docker (Testcontainers).
/// Enable with <c>NEXO_RUN_DYNAMODB_CONTAINER=1</c> (requires Docker). CI and default local runs skip these tests.
/// </summary>
[Collection("IngressDynamoDbDocker")]
[Trait("Category", "DockerOptional")]
public sealed class DynamoDbSmsIngressDockerTests
{
    private readonly IngressDynamoDbDockerFixture _fixture;
    private readonly ITestOutputHelper _output;

    public DynamoDbSmsIngressDockerTests(IngressDynamoDbDockerFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact(Timeout = 180_000)]
    public async Task Dynamo_store_records_and_replays_idempotently()
    {
        if (_fixture.Store is null)
        {
            _output.WriteLine("Skipping DynamoDB Local integration: set NEXO_RUN_DYNAMODB_CONTAINER=1 with Docker available.");
            return;
        }

        var r1 = await _fixture.Store.TryRecordApprovalAsync("+15555550100", "tok-dc", "SM-dc-1", CancellationToken.None);
        r1.Accepted.Should().BeTrue();
        r1.IdempotentReplay.Should().BeFalse();

        var r2 = await _fixture.Store.TryRecordApprovalAsync("+15555550100", "tok-dc", "SM-dc-1", CancellationToken.None);
        r2.IdempotentReplay.Should().BeTrue();
    }
}

/// <summary>Shared DynamoDB Local container + table for <see cref="DynamoDbSmsIngressDockerTests"/>.</summary>
public sealed class IngressDynamoDbDockerFixture : IAsyncLifetime
{
    private DynamoDbContainer? _container;
    private AmazonDynamoDBClient? _client;

    public string TableName { get; } = "nexo_sms_ingress_" + Guid.NewGuid().ToString("N");

    public DynamoDbSmsIngressApprovalStore? Store { get; private set; }

    public async Task InitializeAsync()
    {
        if (!ShouldRunDockerDynamoTests())
            return;

        _container = new DynamoDbBuilder("amazon/dynamodb-local:2.5.2").Build();
        await _container.StartAsync().ConfigureAwait(false);

        var serviceUrl = _container.GetConnectionString();
        _client = new AmazonDynamoDBClient(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });

        await _client.CreateTableAsync(
            new CreateTableRequest
            {
                TableName = TableName,
                BillingMode = BillingMode.PAY_PER_REQUEST,
                KeySchema =
                [
                    new KeySchemaElement(DynamoDbSmsIngressApprovalStore.AttributePartitionKey, KeyType.HASH),
                    new KeySchemaElement(DynamoDbSmsIngressApprovalStore.AttributeSortKey, KeyType.RANGE),
                ],
                AttributeDefinitions =
                [
                    new AttributeDefinition(DynamoDbSmsIngressApprovalStore.AttributePartitionKey, ScalarAttributeType.S),
                    new AttributeDefinition(DynamoDbSmsIngressApprovalStore.AttributeSortKey, ScalarAttributeType.S),
                ],
            },
            CancellationToken.None).ConfigureAwait(false);

        await WaitForTableActiveAsync(_client, TableName, CancellationToken.None).ConfigureAwait(false);

        var options = Options.Create(new SmsIngressDynamoDbOptions { TableName = TableName });
        var monitor = new StaticOptionsMonitor(options);
        Store = new DynamoDbSmsIngressApprovalStore(_client, monitor, NullLogger<DynamoDbSmsIngressApprovalStore>.Instance);
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            try
            {
                await _client.DeleteTableAsync(TableName, CancellationToken.None).ConfigureAwait(false);
            }
            catch (ResourceNotFoundException)
            {
            }

            _client.Dispose();
            _client = null;
        }

        if (_container is not null)
        {
            await _container.DisposeAsync().ConfigureAwait(false);
            _container = null;
        }

        Store = null;
    }

    private static bool ShouldRunDockerDynamoTests()
    {
        var flag = Environment.GetEnvironmentVariable("NEXO_RUN_DYNAMODB_CONTAINER");
        if (string.Equals(flag, "0", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "false", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(flag, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WaitForTableActiveAsync(IAmazonDynamoDB client, string tableName, CancellationToken cancellationToken)
    {
        for (var i = 0; i < 60; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var d = await client.DescribeTableAsync(tableName, cancellationToken).ConfigureAwait(false);
            if (d.Table.TableStatus == TableStatus.ACTIVE)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new TimeoutException($"DynamoDB table {tableName} did not become ACTIVE in time.");
    }

    private sealed class StaticOptionsMonitor : IOptionsMonitor<SmsIngressDynamoDbOptions>
    {
        private readonly IOptions<SmsIngressDynamoDbOptions> _options;

        public StaticOptionsMonitor(IOptions<SmsIngressDynamoDbOptions> options) => _options = options;

        public SmsIngressDynamoDbOptions CurrentValue => _options.Value;

        public SmsIngressDynamoDbOptions Get(string? name) => _options.Value;

        public IDisposable? OnChange(Action<SmsIngressDynamoDbOptions, string?> listener) => null;
    }
}

[CollectionDefinition("IngressDynamoDbDocker")]
public sealed class IngressDynamoDbDockerCollection : ICollectionFixture<IngressDynamoDbDockerFixture>;
