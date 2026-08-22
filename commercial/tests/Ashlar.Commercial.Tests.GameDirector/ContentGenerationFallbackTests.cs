using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Ashlar.Abstractions;
using Ashlar.Core.Domain.Bricks;
using Ashlar.Core.Domain.Execution;
using Xunit;

namespace Ashlar.Tests.GameDirector;

/// <summary>Tests for content generation fallback.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class ContentGenerationFallbackTests
{
    [Fact]
    public async Task Model_exception_falls_back_to_template()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new ThrowingModel());

        var input = new BrickInput();
        input.Set("session_id", "sess");
        input.Set("prompt", "macro line");
        input.Set("target_type", "macro");
        input.Set("fast", false);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        output.Get<string>("content").Should().Contain("[macro]");
        output.Get<string>("content").Should().Contain("macro line");
    }

    [Fact]
    public async Task Null_model_uses_offline_template()
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            model: null);

        var input = new BrickInput();
        input.Set("session_id", "sess");
        input.Set("prompt", "flavor please");
        input.Set("target_type", "flavor");
        input.Set("fast", false);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        output.Get<string>("content").Should().Contain("flavor please");
        output.Get<string>("model_used").Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ForgeStateSessionReader_returns_snapshot_for_same_session_id()
    {
        var reader = new ForgeStateSessionReader(() => "sess-a", () => "neon");
        var snapshot = reader.GetSession("sess-a");
        snapshot.Should().NotBeNull();
        snapshot!.SessionId.Should().Be("sess-a");
        snapshot.AestheticSummary.Should().Be("neon");
    }

    [Fact]
    public void ForgeStateSessionReader_returns_requested_session_when_different()
    {
        var reader = new ForgeStateSessionReader(() => "current", () => "noir");
        var snapshot = reader.GetSession("other");
        snapshot!.SessionId.Should().Be("other");
        snapshot.AestheticSummary.Should().Be("noir");
    }

    [Fact]
    public void ForgeStateSessionReader_empty_request_falls_back_to_current()
    {
        var reader = new ForgeStateSessionReader(() => "current", () => null);
        var snapshot = reader.GetSession("");
        snapshot!.SessionId.Should().Be("current");
        snapshot.AestheticSummary.Should().BeNull();
    }

    /// <summary>Throwing model.</summary>
    private sealed class ThrowingModel : IModel
    {
        /// <summary>Complete async.</summary>
        /// <param name="input">Input.</param>
        /// <param name="ct">Cancellation token.</param>
        public Task<ModelOutput> CompleteAsync(ModelInput input, CancellationToken ct) =>
            /// <summary>Invalid operation exception.</summary>
            /// <param name="outage"">Outage".</param>
            throw new InvalidOperationException("simulated outage");
    }
}
