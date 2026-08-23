using FluentAssertions;
using Ashlar.BackgroundAgents.Trust;
using Ashlar.BackgroundAgents.WebSearch;
using Xunit;

namespace Ashlar.Tests.BackgroundAgents.Trust;

/// <summary>Tests for cloud sanitization proxy.</summary>
public class CloudSanitizationProxyTests
{
    [Fact]
    public void SanitizeForCloud_WhenAirGapped_AllowsWithoutFiltering()
    {
        var proxy = new CloudSanitizationProxy(new SensitiveContentFilter());

        var context = new OutgoingContext
        {
            SystemPrompt = "System",
            UserPrompt = "user@example.com",
            IsAirGapped = true,
        };

        var result = proxy.SanitizeForCloud(context);

        result.Allowed.Should().BeTrue();
        result.SanitizedContext!.UserPrompt.Should().Be("user@example.com");
    }

    [Fact]
    public void SanitizeForCloud_WhenPiiInPrompt_Blocks()
    {
        var proxy = new CloudSanitizationProxy(new SensitiveContentFilter());

        var context = new OutgoingContext
        {
            SystemPrompt = "System",
            UserPrompt = "contact user@example.com for help",
            IsAirGapped = false,
        };

        var result = proxy.SanitizeForCloud(context);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Contain("PII");
    }

    [Fact]
    public void SanitizeForCloud_WhenNoPii_AllowsWithRedaction()
    {
        var proxy = new CloudSanitizationProxy(new SensitiveContentFilter());
        var auditLog = new InMemorySanitizationAuditLog();
        proxy = new CloudSanitizationProxy(new SensitiveContentFilter(), null, auditLog);

        var context = new OutgoingContext
        {
            SystemPrompt = "System",
            UserPrompt = "What is the weather?",
            IsAirGapped = false,
        };

        var result = proxy.SanitizeForCloud(context);

        result.Allowed.Should().BeTrue();
        result.SanitizedContext!.UserPrompt.Should().Be("What is the weather?");
    }

    [Fact]
    public void SanitizeForCloud_WhenNoFilter_Allows()
    {
        var proxy = new CloudSanitizationProxy(contentFilter: null);

        var context = new OutgoingContext
        {
            SystemPrompt = "S",
            UserPrompt = "user@test.com",
            IsAirGapped = false,
        };

        var result = proxy.SanitizeForCloud(context);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public void SanitizeForCloud_WhenPiiRedacted_LogsRedaction()
    {
        var filter = new SensitiveContentFilter(blockQueriesWithPii: false);
        var auditLog = new InMemorySanitizationAuditLog();
        var proxy = new CloudSanitizationProxy(filter, null, auditLog);

        var context = new OutgoingContext
        {
            SystemPrompt = "Analyze",
            UserPrompt = "Email: user@example.com",
            IsAirGapped = false,
        };

        var result = proxy.SanitizeForCloud(context);

        result.Allowed.Should().BeTrue();
        result.SanitizedContext!.UserPrompt.Should().Contain("[REDACTED]");
        var entries = auditLog.GetRecent(10);
        entries.Should().NotBeEmpty();
    }
}
