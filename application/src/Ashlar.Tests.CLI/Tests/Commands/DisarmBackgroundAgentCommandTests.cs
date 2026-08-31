using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.BackgroundAgents.Configuration;
using Ashlar.CLI.Commands.BackgroundAgent;
using Xunit;

namespace Ashlar.Tests.CLI.Tests.Commands;

/// <summary>
/// A4 emergency stop: <c>background-agent disarm</c> forces the mode to Passive so every extender
/// halts on its next cycle. Pins that it disarms regardless of the prior mode and returns success.
/// </summary>
public sealed class DisarmBackgroundAgentCommandTests
{
    private static ModeBackgroundAgentCommand NewCmd(IAggressivenessModeStore store) =>
        new(store, NullLogger<ModeBackgroundAgentCommand>.Instance);

    [Fact]
    public async Task Disarm_fromActive_setsPassive()
    {
        var store = new InMemoryAggressivenessModeStore();
        store.SetMode(BackgroundAgentAggressivenessMode.Active);

        var rc = await NewCmd(store).DisarmAsync(reason: "spend spike", formatJson: false);

        rc.Should().Be(0);
        store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);
    }

    [Fact]
    public async Task Disarm_whenAlreadyPassive_isIdempotent()
    {
        var store = new InMemoryAggressivenessModeStore();

        var rc = await NewCmd(store).DisarmAsync(reason: null, formatJson: true);

        rc.Should().Be(0);
        store.GetMode().Should().Be(BackgroundAgentAggressivenessMode.Passive);
    }
}
