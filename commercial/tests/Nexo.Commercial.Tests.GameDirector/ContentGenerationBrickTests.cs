using FluentAssertions;
using GameDirector.Bricks;
using GameDirector.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Nexo.Core.Domain.Bricks;
using Nexo.Core.Domain.Execution;
using Xunit;

namespace Nexo.Tests.GameDirector;

/// <summary>Tests for content generation brick.</summary>
[Trait("Category", "GameDirectorApplication")]
public sealed class ContentGenerationBrickTests
{
    [Fact]
    public async Task Offline_model_produces_content_without_network()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var brick = new ContentGenerationBrick(
            audit,
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("session_id", "sess-1");
        input.Set("prompt", "Write flavor for a sci-fi arena");
        input.Set("target_type", "flavor");
        input.Set("fast", true);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        output.Get<string>("content").Should().NotBeNullOrWhiteSpace();
        output.Get<string>("model_used").Should().NotBeNullOrWhiteSpace();
        output.Get<int>("tokens").Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("flavor")]
    [InlineData("item")]
    [InlineData("macro")]
    public async Task All_target_types_execute(string targetType)
    {
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            new GameDirectorOfflineModel());

        var input = new BrickInput();
        input.Set("session_id", "sess-types");
        input.Set("prompt", $"generate {targetType}");
        input.Set("target_type", targetType);
        input.Set("fast", false);

        var output = await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        output.Get<string>("content").Should().Contain(targetType, because: "prompt is echoed in offline path");
    }

    [Fact]
    public async Task Forge_session_reader_injects_aesthetic_into_prompt()
    {
        var audit = GameDirectorTestHost.CreateAuditLog();
        var model = new GameDirectorTestHost.CapturingModel();
        var sessions = new ForgeStateSessionReader(
            () => "forge-sess-99",
            () => "cyberpunk-neon");
        var brick = new ContentGenerationBrick(audit, NullLogger<ContentGenerationBrick>.Instance, model, sessions);

        var input = new BrickInput();
        input.Set("session_id", "forge-sess-99");
        input.Set("prompt", "describe the hub district");
        input.Set("target_type", "flavor");

        await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        model.Prompts.Should().ContainMatch("*cyberpunk-neon*");
    }

    [Fact]
    public async Task Strips_api_key_from_prompt_before_model_call()
    {
        var model = new GameDirectorTestHost.CapturingModel();
        var brick = new ContentGenerationBrick(
            GameDirectorTestHost.CreateAuditLog(),
            NullLogger<ContentGenerationBrick>.Instance,
            model);

        var input = new BrickInput();
        input.Set("session_id", "s");
        input.Set("prompt", "use api_key=secret123 for auth");
        input.Set("target_type", "item");

        await brick.ExecuteAsync(input, ImplementationType.Agentic, GameDirectorTestHost.CreateContext());

        model.Prompts.Should().ContainMatch("*[redacted]*").And.NotContain("secret123");
    }
}
