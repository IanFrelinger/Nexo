using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Ashlar.Contracts;
using Ashlar.Ingress.DynamoDb;
using Ashlar.Tests.Infrastructure.Helpers;
using Testcontainers.DynamoDb;
using Xunit;
using Xunit.Abstractions;

namespace Ashlar.Tests.Infrastructure.Tests.Ingress;

/// <summary>
/// Optional integration tests against DynamoDB Local in Docker (Testcontainers).
/// Enable with <c>ASHLAR_RUN_DYNAMODB_CONTAINER=1</c> (requires Docker). CI and default local runs report these
/// tests as Skipped (see <see cref="OptInFactAttribute"/>); the fixture stays a no-op until opted in.
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

    [OptInFact("ASHLAR_RUN_DYNAMODB_CONTAINER", "DynamoDB Local in Docker (Testcontainers)", Timeout = 180_000)]
    public async Task Dynamo_store_records_and_replays_idempotently()
    {
        // Opted in but the fixture produced no store (Docker unavailable / container failed): keep the
        // legacy soft return for the post-opt-in runtime case; the discovery-time gate is the attribute.
        if (_fixture.Store is null)
        {
            _output.WriteLine("Skipping DynamoDB Local integration: ASHLAR_RUN_DYNAMODB_CONTAINER=1 is set but the DynamoDB Local container did not start.");
            return;
        }

        var r1 = await _fixture.Store.TryRecordApprovalAsync("+15555550100", "tok-dc", "SM-dc-1", CancellationToken.None);
        r1.Accepted.Should().BeTrue();
        r1.IdempotentReplay.Should().BeFalse();

        var r2 = await _fixture.Store.TryRecordApprovalAsync("+15555550100", "tok-dc", "SM-dc-1", CancellationToken.None);
        r2.IdempotentReplay.Should().BeTrue();
    }
}
