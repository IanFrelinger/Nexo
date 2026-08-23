using FluentAssertions;
using GameDirector.Bricks;
using Ashlar.Abstractions;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for game director offline model.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class GameDirectorOfflineModelTests
{
    [Fact]
    public async Task CompleteAsync_prefixes_offline_marker()
    {
        var model = new GameDirectorOfflineModel();
        var output = await model.CompleteAsync(
            new ModelInput([("user", "balance rationale")]),
            CancellationToken.None);

        output.Text.Should().StartWith("[offline]");
        output.Text.Should().Contain("balance rationale");
    }
}
