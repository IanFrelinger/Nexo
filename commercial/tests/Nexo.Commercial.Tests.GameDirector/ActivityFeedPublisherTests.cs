using FluentAssertions;
using GameDirector.Agents;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for activity feed publisher.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class ActivityFeedPublisherTests
{
    [Fact]
    public async Task AuditLogPublisher_writes_classification_with_source_and_summary()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var publisher = new AuditLogActivityFeedPublisher(audit);

        await publisher.PublishAsync("balance-watcher", "drift", "ttk spread 1200ms", CancellationToken.None);

        var entries = audit.GetRecent(10, eventType: "Classification");
        entries.Should().Contain(e =>
            e.DataType == "game-director-activity" &&
            e.LevelName == "drift" &&
            e.Reason != null && e.Reason.Contains("balance-watcher") && e.Reason.Contains("ttk spread"));
    }
}
