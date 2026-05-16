using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Nexo.Ingress.AwsSns.Tests;

public sealed class SnsCanonicalStringBuilderTests
{
    [Fact]
    public void Notification_without_subject_matches_aws_canonical_order()
    {
        const string json = """
            {
              "Type": "Notification",
              "MessageId": "4d4dc071-ddbf-465d-bba8-08f81c89da64",
              "TopicArn": "arn:aws:sns:us-east-2:123456789012:s4-MySNSTopic-1G1WEFCOXTC0P",
              "Message": "My Test Message",
              "Timestamp": "2019-01-31T04:37:04.321Z"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var ok = SnsCanonicalStringBuilder.TryBuild(doc.RootElement, out var s, out var err);
        ok.Should().BeTrue($"canonical build failed: {err}");
        s.Should().Be(
            "Message\nMy Test Message\nMessageId\n4d4dc071-ddbf-465d-bba8-08f81c89da64\nTimestamp\n2019-01-31T04:37:04.321Z\nTopicArn\narn:aws:sns:us-east-2:123456789012:s4-MySNSTopic-1G1WEFCOXTC0P\nType\nNotification");
    }

    [Fact]
    public void Notification_with_subject_includes_subject_in_sorted_keys()
    {
        const string json = """
            {
              "Type": "Notification",
              "MessageId": "4d4dc071-ddbf-465d-bba8-08f81c89da64",
              "TopicArn": "arn:aws:sns:us-east-2:123456789012:s4-MySNSTopic-1G1WEFCOXTC0P",
              "Subject": "My subject",
              "Message": "My Test Message",
              "Timestamp": "2019-01-31T04:37:04.321Z"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var ok = SnsCanonicalStringBuilder.TryBuild(doc.RootElement, out var s, out var err);
        ok.Should().BeTrue($"canonical build failed: {err}");
        s.Should().Be(
            "Message\nMy Test Message\nMessageId\n4d4dc071-ddbf-465d-bba8-08f81c89da64\nSubject\nMy subject\nTimestamp\n2019-01-31T04:37:04.321Z\nTopicArn\narn:aws:sns:us-east-2:123456789012:s4-MySNSTopic-1G1WEFCOXTC0P\nType\nNotification");
    }

    [Fact]
    public void Subscription_confirmation_orders_fields_lexicographically()
    {
        const string json = """
            {
              "Type": "SubscriptionConfirmation",
              "MessageId": "mid",
              "Token": "tok",
              "TopicArn": "arn:aws:sns:us-east-1:1:t",
              "Message": "Please confirm",
              "SubscribeURL": "https://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription",
              "Timestamp": "2024-01-01T00:00:00.000Z"
            }
            """;

        using var doc = JsonDocument.Parse(json);
        var ok = SnsCanonicalStringBuilder.TryBuild(doc.RootElement, out var s, out var err);
        ok.Should().BeTrue($"canonical build failed: {err}");
        s.Should().Be(
            "Message\nPlease confirm\nMessageId\nmid\nSubscribeURL\nhttps://sns.us-east-1.amazonaws.com/?Action=ConfirmSubscription\nTimestamp\n2024-01-01T00:00:00.000Z\nToken\ntok\nTopicArn\narn:aws:sns:us-east-1:1:t\nType\nSubscriptionConfirmation");
    }
}
