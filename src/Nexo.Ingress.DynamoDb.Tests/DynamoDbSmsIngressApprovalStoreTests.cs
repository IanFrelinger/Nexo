using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Nexo.Ingress.DynamoDb;
using Xunit;

namespace Nexo.Ingress.DynamoDb.Tests;

/// <summary>Tests for dynamo db sms ingress approval store.</summary>
public sealed class DynamoDbSmsIngressApprovalStoreTests
{
    [Fact]
    public async Task First_put_returns_accepted_response()
    {
        var dynamo = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
        dynamo.Setup(d => d.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutItemResponse());
        var opts = Options.Create(new Nexo.Contracts.SmsIngressDynamoDbOptions { TableName = "t1" });
        var monitor = new OptionsMonitorStub(opts);
        var sut = new DynamoDbSmsIngressApprovalStore(dynamo.Object, monitor, NullLogger<DynamoDbSmsIngressApprovalStore>.Instance);

        var r = await sut.TryRecordApprovalAsync("+1", "tok", "SM1", CancellationToken.None);

        r.Accepted.Should().BeTrue();
        r.ApprovalToken.Should().Be("tok");
        r.IdempotentReplay.Should().BeFalse();
        dynamo.Verify(d => d.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Conditional_failure_replays_from_get_item()
    {
        var dynamo = new Mock<IAmazonDynamoDB>(MockBehavior.Strict);
        dynamo.Setup(d => d.PutItemAsync(It.IsAny<PutItemRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConditionalCheckFailedException("dup"));
        dynamo.Setup(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetItemResponse
            {
                Item = new Dictionary<string, AttributeValue>
                {
                    ["approvalToken"] = new AttributeValue { S = "prior" },
                },
            });
        var opts = Options.Create(new Nexo.Contracts.SmsIngressDynamoDbOptions { TableName = "t1" });
        var monitor = new OptionsMonitorStub(opts);
        var sut = new DynamoDbSmsIngressApprovalStore(dynamo.Object, monitor, NullLogger<DynamoDbSmsIngressApprovalStore>.Instance);

        var r = await sut.TryRecordApprovalAsync("+1", "tok", "SM2", CancellationToken.None);

        r.IdempotentReplay.Should().BeTrue();
        r.ApprovalToken.Should().Be("prior");
        dynamo.Verify(d => d.GetItemAsync(It.IsAny<GetItemRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Options monitor stub.</summary>
    private sealed class OptionsMonitorStub : IOptionsMonitor<Nexo.Contracts.SmsIngressDynamoDbOptions>
    {
        private readonly IOptions<Nexo.Contracts.SmsIngressDynamoDbOptions> _inner;
        /// <summary>Options monitor stub.</summary>
        /// <param name="inner">Inner.</param>
        public OptionsMonitorStub(IOptions<Nexo.Contracts.SmsIngressDynamoDbOptions> inner) => _inner = inner;
        public Nexo.Contracts.SmsIngressDynamoDbOptions CurrentValue => _inner.Value;
        /// <summary>Gets the value.</summary>
        /// <param name="name">Name.</param>
        public Nexo.Contracts.SmsIngressDynamoDbOptions Get(string? name) => _inner.Value;
        /// <summary>On change.</summary>
        /// <param name="listener">Listener.</param>
        public IDisposable? OnChange(Action<Nexo.Contracts.SmsIngressDynamoDbOptions, string?> listener) => null;
    }
}
