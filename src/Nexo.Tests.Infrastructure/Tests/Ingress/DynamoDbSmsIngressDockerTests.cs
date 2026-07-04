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
