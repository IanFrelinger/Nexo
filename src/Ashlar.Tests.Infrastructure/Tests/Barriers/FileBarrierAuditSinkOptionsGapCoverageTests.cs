using FluentAssertions;
using Ashlar.Runtime.Barriers.Identity;
using Ashlar.Runtime.Barriers.Identity.Resolvers;
using Ashlar.Runtime.Barriers.Sinks;
using Xunit;

namespace Ashlar.Tests.Infrastructure.Tests.Barriers;

/// <summary>Tests for file barrier audit sink options gap coverage.</summary>
public sealed class FileBarrierAuditSinkOptionsGapCoverageTests
{
    [Fact]
    public void Defaults_match_expected_audit_sink_configuration()
    {
        var options = new FileBarrierAuditSinkOptions();

        options.Directory.Should().Be("audit");
        options.FilePrefix.Should().Be("audit-barriers");
        options.MaxFileSizeBytes.Should().Be(10 * 1024 * 1024);
        options.MaxRotatedFiles.Should().Be(10);
        options.FlushIntervalMs.Should().Be(5_000);
        options.FlushEveryEvent.Should().BeFalse();
        options.ChannelCapacity.Should().Be(4096);
    }
}
