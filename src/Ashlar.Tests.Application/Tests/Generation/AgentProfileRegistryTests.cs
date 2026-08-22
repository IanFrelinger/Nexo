using FluentAssertions;
using Ashlar.Core.Application.Generation;
using Ashlar.Core.Application.Orchestration;
using Ashlar.Core.Domain.Bricks.Ports;
using Xunit;

namespace Ashlar.Tests.Application.Tests.Generation;

/// <summary>
/// The registry is the last place a profile can be rejected before it starts
/// minting verdicts. Everything here guards the same invariant: a profile may
/// only register if a real gate will actually run.
/// </summary>
public sealed class AgentProfileRegistryTests
{
    [Fact]
    public void Register_rejects_a_profile_with_zero_validators()
    {
        var registry = new AgentProfileRegistry();

        var act = () => registry.Register(new AgentProfile
        {
            TargetId = "ungated",
            Drafter = new StubDrafter(),
            Validators = Array.Empty<object>()
        });

        // Without this the profile registers, the deterministic branch mints
        // verified:true unconditionally, and the caller is handed a verdict that
        // no gate ever produced.
        act.Should().Throw<ArgumentException>()
            .WithMessage("*no usable validators*");
        registry.Resolve("ungated").Should().BeNull("a rejected profile must not be half-registered");
        registry.TargetIds.Should().BeEmpty();
    }

    [Fact]
    public void Register_rejects_a_profile_whose_only_validator_is_mistyped()
    {
        var registry = new AgentProfileRegistry();

        var act = () => registry.Register(new AgentProfile
        {
            TargetId = "mistyped",
            Drafter = new StubDrafter(),
            Validators = new object[] { new NotAPostValidator() }
        });

        // A mistyped validator is filtered out by the engine, leaving zero gates —
        // the same hole reached from the other side. The mistyped-validator message
        // is the more actionable of the two, so it must win.
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot use*");
        registry.TargetIds.Should().BeEmpty();
    }

    [Fact]
    public void Register_accepts_a_properly_gated_profile()
    {
        var registry = new AgentProfileRegistry();
        var profile = new AgentProfile
        {
            TargetId = "gated",
            Drafter = new StubDrafter(),
            Validators = new object[] { new AlwaysOkValidator() }
        };

        registry.Register(profile);

        registry.Resolve("gated").Should().BeSameAs(profile);
        registry.TargetIds.Should().ContainSingle().Which.Should().Be("gated");
    }

    [Fact]
    public void Register_accepts_a_profile_that_declares_a_usable_validator_alongside_extras()
    {
        // Regression guard on the shape of the check: it asks whether at least one
        // USABLE validator exists, so it must not be satisfied by count alone.
        var registry = new AgentProfileRegistry();

        var act = () => registry.Register(new AgentProfile
        {
            TargetId = "partially-gated",
            Drafter = new StubDrafter(),
            Validators = new object[] { new AlwaysOkValidator(), new NotAPostValidator() }
        });

        act.Should().Throw<ArgumentException>("a validator the engine skips is still a hole")
            .WithMessage("*cannot use*");
    }

    private sealed class StubDrafter : IArtifactDrafter
    {
        public Task<GeneratedArtifact> DraftAsync(
            GenerationRequest request,
            IReadOnlyList<string> priorFeedback,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new GeneratedArtifact { Content = "x" });
    }

    private sealed class AlwaysOkValidator : IPostValidator<GeneratedArtifact>
    {
        public ValueTask<(bool ok, string? reason)> ValidateAsync(
            GeneratedArtifact output, CancellationToken ct) =>
            new((true, (string?)null));
    }

    private sealed class NotAPostValidator
    {
    }
}
